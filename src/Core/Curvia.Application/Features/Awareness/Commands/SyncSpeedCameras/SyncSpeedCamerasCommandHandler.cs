using Microsoft.Extensions.Logging;
using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

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
/// </summary>
internal sealed class SyncSpeedCamerasCommandHandler : ICommandHandler<SyncSpeedCamerasCommand>
{
	#region Constants
	/// <summary>
	/// Maximum distance in metres between two cameras from different sources to be considered the same physical camera.
	/// </summary>
	private const double DeduplicationDistanceMeters = 50.0;

	/// <summary>
	/// Earth's approximate radius in metres, used in the Haversine distance calculation.
	/// </summary>
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
	/// <returns>A success result when the sync completes without fatal error.</returns>
	public async Task<Result> Handle(SyncSpeedCamerasCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();
		_logger.LogInformation("Speed-camera sync started for country {Country}.", country);

		// Resilient parallel fetch — one provider failing does not abort the others.
		var allRecords = await FetchFromAllProvidersAsync(country, ct);

		if (allRecords.Count == 0)
		{
			_logger.LogWarning("Speed-camera sync for {Country}: no records returned from any provider.", country);
			return Result.Success();
		}

		// Deduplicate cameras within 50 m + same direction from different sources.
		var deduplicated = DeduplicateCameras(allRecords);
		_logger.LogInformation("Speed-camera sync for {Country}: {Total} raw records → {Deduped} after deduplication.", country, allRecords.Count, deduplicated.Count);

		// Delete existing data for this country per source then reload.
		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		// Upsert all deduplicated cameras.
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
				existing.Sync(record.Latitude, record.Longitude, record.SpeedLimitKmh, record.Direction);
				await _repository.UpdateAsync(existing, ct);
				updated++;
			}
		}

		_logger.LogInformation("Speed-camera sync for {Country} complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped.", country, inserted, updated, skipped);
		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches speed-camera records from all providers in parallel.
	/// Each provider is wrapped in an individual try/catch so that one
	/// failing provider (HTTP timeout, parse error, etc.) does not cancel
	/// results from the other providers.
	/// </summary>
	/// <param name="country">ISO 3166-1 alpha-2 country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Combined list of raw camera records from all providers that succeeded.</returns>
	private async Task<List<SpeedCameraRecord>> FetchFromAllProvidersAsync(string country, CancellationToken ct)
	{
		var tasks = _providers.Select(async provider =>
		{
			try
			{
				return await provider.FetchAsync(country, ct);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Speed-camera provider {Provider} failed for country {Country}.", provider.ProviderName, country);
				return Enumerable.Empty<SpeedCameraRecord>();
			}
		}).ToList();

		var results = await Task.WhenAll(tasks);
		return results.SelectMany(r => r).ToList();
	}

	/// <summary>
	/// Clusters camera records from different sources. If two cameras from different
	/// sources are within <see cref="DeduplicationDistanceMeters"/> metres and have the
	/// same (or Unknown) direction, they are considered the same physical camera.
	/// The OSM record wins when available; otherwise the first record in the cluster is kept.
	/// Cameras from the same source are never deduplicated against each other.
	/// </summary>
	/// <param name="records">All raw camera records from all providers.</param>
	/// <returns>A deduplicated list where each physical camera appears once.</returns>
	private static List<SpeedCameraRecord> DeduplicateCameras(List<SpeedCameraRecord> records)
	{
		var kept = new List<SpeedCameraRecord>();

		foreach (var candidate in records)
		{
			var duplicate = kept.FirstOrDefault(k =>
				k.Source != candidate.Source &&
				(k.Direction == candidate.Direction || k.Direction == CameraDirection.Unknown || candidate.Direction == CameraDirection.Unknown) &&
				HaversineMeters(k.Latitude, k.Longitude, candidate.Latitude, candidate.Longitude) <= DeduplicationDistanceMeters);

			if (duplicate is null)
			{
				kept.Add(candidate);
				continue;
			}

			// OSM record wins; replace the duplicate if it was not already OSM.
			if (candidate.Source.Equals("osm", StringComparison.OrdinalIgnoreCase) && !duplicate.Source.Equals("osm", StringComparison.OrdinalIgnoreCase))
			{
				kept.Remove(duplicate);
				kept.Add(candidate);
			}
		}

		return kept;
	}

	/// <summary>
	/// Returns the great-circle distance in metres between two WGS84 points using the Haversine formula.
	/// </summary>
	/// <param name="lat1">Latitude of point 1 in decimal degrees.</param>
	/// <param name="lon1">Longitude of point 1 in decimal degrees.</param>
	/// <param name="lat2">Latitude of point 2 in decimal degrees.</param>
	/// <param name="lon2">Longitude of point 2 in decimal degrees.</param>
	/// <returns>Distance in metres.</returns>
	private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = (lat2 - lat1) * Math.PI / 180.0;
		var dLon = (lon2 - lon1) * Math.PI / 180.0;
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
	}
	#endregion
}