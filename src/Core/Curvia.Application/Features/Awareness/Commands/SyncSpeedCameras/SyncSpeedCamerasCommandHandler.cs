using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncSpeedCamerasCommand"/>.
///              Fetches speed-camera records from all providers in parallel (resilient — one
///              failing provider does not abort the others), deduplicates cameras from different
///              sources that are within 50 m of each other, deletes existing data per source,
///              then upserts the deduplicated dataset.
///              Runs weekly per country — speed camera locations change infrequently.
///
///              A single SaveChangesAsync is called at the end of the pipeline so that
///              the entire sync for a country (deletes + inserts + updates) is committed
///              atomically in one round-trip.
///
///              Returns Result.Failure when one or more providers fail so that Hangfire marks
///              the per-country job as failed and retries it automatically.
/// </summary>
internal sealed class SyncSpeedCamerasCommandHandler : ICommandHandler<SyncSpeedCamerasCommand>
{
	#region Constants
	/// <summary>Maximum distance in metres between two cameras from different sources to be considered the same physical camera.</summary>
	private const double DeduplicationDistanceMeters = 50.0;

	/// <summary>Earth's approximate radius in metres, used in the Haversine distance calculation.</summary>
	private const double EarthRadiusMeters = 6_371_000.0;
	#endregion

	#region Dependencies
	/// <summary>All registered speed-camera data providers.</summary>
	private readonly IEnumerable<ISpeedCameraDataProvider> _providers;

	/// <summary>Persistence contract for <see cref="SpeedCamera"/> aggregates.</summary>
	private readonly ISpeedCameraRepository _repository;

	/// <summary>Commits all pending changes atomically: auditing + outbox + SaveChanges.</summary>
	private readonly IUnitOfWork _unitOfWork;

	/// <summary>Logger for this handler.</summary>
	private readonly ILogger<SyncSpeedCamerasCommandHandler> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncSpeedCamerasCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered speed-camera data providers.</param>
	/// <param name="repository">Persistence contract for speed-camera aggregates.</param>
	/// <param name="unitOfWork">Unit of work used to commit the sync atomically.</param>
	/// <param name="logger">Logger instance.</param>
	public SyncSpeedCamerasCommandHandler(IEnumerable<ISpeedCameraDataProvider> providers, ISpeedCameraRepository repository, IUnitOfWork unitOfWork, ILogger<SyncSpeedCamerasCommandHandler> logger)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the speed-camera sync pipeline: fetch in parallel (resilient), deduplicate, delete existing per source, then upsert.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A success result when all providers succeed and the sync completes.
	/// A failure result when one or more providers fail — Hangfire will mark the job as failed.
	/// </returns>
	public async Task<Result> Handle(SyncSpeedCamerasCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();
		_logger.LogInformation("Speed-camera sync started for country {Country}.", country);

		var (allRecords, hadProviderFailure, failedProviders) = await FetchFromAllProvidersAsync(country, ct);

		if (allRecords.Count == 0 && !hadProviderFailure)
		{
			_logger.LogWarning("Speed-camera sync for {Country}: no records returned from any provider.", country);
			return Result.Success();
		}

		var deduplicated = DeduplicateCameras(allRecords);
		_logger.LogInformation("Speed-camera sync for {Country}: {Total} raw records → {Deduped} after deduplication.", country, allRecords.Count, deduplicated.Count);

		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		var inserted = 0;
		var updated = 0;
		var skipped = 0;

		foreach (var record in deduplicated)
		{
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, record.Source, country, ct);

			if (existing is null)
			{
				var cameraResult = SpeedCamera.Create(record.ExternalId, record.Source, country, record.Latitude, record.Longitude, record.SpeedLimitKmh, record.Direction);
				if (cameraResult.IsFailure)
				{
					_logger.LogWarning("Speed-camera sync for {Country}: failed to create camera {ExternalId} — {Error}.", country, record.ExternalId, cameraResult.Error);
					skipped++;
					continue;
				}
				await _repository.AddAsync(cameraResult.Value, ct);
				inserted++;
			}
			else
			{
				var syncResult = existing.Sync(record.Latitude, record.Longitude, record.SpeedLimitKmh, record.Direction);
				if (syncResult.IsFailure)
				{
					_logger.LogWarning("Speed-camera sync for {Country}: failed to sync camera {ExternalId} — {Error}.", country, record.ExternalId, syncResult.Error);
					skipped++;
					continue;
				}
				await _repository.UpdateAsync(existing, ct);
				updated++;
			}
		}

		// Single commit for the entire country sync — all deletes, inserts and updates in one atomic round-trip.
		var saveResult = await _unitOfWork.SaveChangesAsync(ct);
		if (saveResult.IsFailure)
			return saveResult;

		_logger.LogInformation("Speed-camera sync for {Country} complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped.", country, inserted, updated, skipped);

		// Propagate provider failures so Hangfire marks the per-country job as failed.
		if (hadProviderFailure)
			return Result.Failure(new Error("ProviderError", $"Speed-camera sync for {country} had failing provider(s): {string.Join(", ", failedProviders)}."));

		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches speed-camera records from all providers in parallel.
	/// Individual provider failures are caught and logged — they do not abort the sync.
	/// </summary>
	/// <param name="country">Uppercase ISO country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A tuple of: the flat list of all records from successful providers,
	/// a flag indicating whether any provider failed, and the list of failed provider names.
	/// </returns>
	private async Task<(List<SpeedCameraRecord> Records, bool HadFailure, List<string> FailedProviders)> FetchFromAllProvidersAsync(string country, CancellationToken ct)
	{
		var failedProviders = new List<string>();

		var tasks = _providers.Select(async p =>
		{
			try
			{
				var records = await p.FetchAsync(country, ct);
				return (Records: records, Failed: false, ProviderName: p.ProviderName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Speed-camera provider {Provider} failed for {Country}.", p.ProviderName, country);
				return (Records: (IReadOnlyList<SpeedCameraRecord>)Array.Empty<SpeedCameraRecord>(), Failed: true, ProviderName: p.ProviderName);
			}
		});

		var results = await Task.WhenAll(tasks);

		foreach (var result in results.Where(r => r.Failed))
			failedProviders.Add(result.ProviderName);

		var allRecords = results.SelectMany(r => r.Records).ToList();
		return (allRecords, failedProviders.Count > 0, failedProviders);
	}

	/// <summary>
	/// Removes duplicate cameras (same physical location, different sources) using Haversine distance.
	/// When two cameras from different sources are within <see cref="DeduplicationDistanceMeters"/>,
	/// the one that appears first in the list is kept.
	/// </summary>
	/// <param name="records">Flat list of all records from all providers.</param>
	/// <returns>Deduplicated list.</returns>
	private static List<SpeedCameraRecord> DeduplicateCameras(List<SpeedCameraRecord> records)
	{
		var kept = new List<SpeedCameraRecord>();

		foreach (var candidate in records)
		{
			var isDuplicate = kept.Any(k =>
				k.Source != candidate.Source &&
				HaversineMeters(k.Latitude, k.Longitude, candidate.Latitude, candidate.Longitude) <= DeduplicationDistanceMeters);

			if (!isDuplicate)
				kept.Add(candidate);
		}

		return kept;
	}

	/// <summary>
	/// Computes the great-circle distance in metres between two WGS84 coordinates using the Haversine formula.
	/// </summary>
	private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = ToRad(lat2 - lat1);
		var dLon = ToRad(lon2 - lon1);
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
	}

	private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
	#endregion
}