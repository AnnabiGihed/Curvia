using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
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

	/// <summary>Logger for this handler.</summary>
	private readonly ILogger<SyncSpeedCamerasCommandHandler> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncSpeedCamerasCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered speed-camera data providers.</param>
	/// <param name="repository">Persistence contract for speed-camera aggregates.</param>
	/// <param name="logger">Logger instance.</param>
	public SyncSpeedCamerasCommandHandler(IEnumerable<ISpeedCameraDataProvider> providers, ISpeedCameraRepository repository, ILogger<SyncSpeedCamerasCommandHandler> logger)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
				// Sync() now returns Result — log and skip on failure (invalid coordinates from source).
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
	private async Task<(List<SpeedCameraRecord> Records, bool HadFailure, List<string> FailedProviders)>
		FetchFromAllProvidersAsync(string country, CancellationToken ct)
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

		foreach (var record in records)
		{
			var isDuplicate = kept.Any(k =>
				k.Source != record.Source &&
				Haversine(k.Latitude, k.Longitude, record.Latitude, record.Longitude) <= DeduplicationDistanceMeters);

			if (!isDuplicate)
				kept.Add(record);
		}

		return kept;
	}

	/// <summary>
	/// Computes the Haversine distance in metres between two WGS84 coordinates.
	/// </summary>
	/// <param name="lat1">Latitude of the first point.</param>
	/// <param name="lon1">Longitude of the first point.</param>
	/// <param name="lat2">Latitude of the second point.</param>
	/// <param name="lon2">Longitude of the second point.</param>
	/// <returns>Distance in metres.</returns>
	private static double Haversine(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = (lat2 - lat1) * Math.PI / 180.0;
		var dLon = (lon2 - lon1) * Math.PI / 180.0;
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
	}
	#endregion
}