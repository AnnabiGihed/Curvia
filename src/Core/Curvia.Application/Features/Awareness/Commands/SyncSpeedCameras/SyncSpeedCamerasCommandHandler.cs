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
///              Fetches speed-camera records from all providers in parallel,
///              deduplicates cameras from different sources that are within 50 m of each other,
///              deletes existing data per source, then upserts the deduplicated dataset.
///              Runs weekly per country — speed camera locations change infrequently.
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
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncSpeedCamerasCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered speed-camera data providers.</param>
	/// <param name="repository">Persistence contract for speed-camera aggregates.</param>
	public SyncSpeedCamerasCommandHandler(IEnumerable<ISpeedCameraDataProvider> providers, ISpeedCameraRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the speed-camera sync pipeline: fetch in parallel, deduplicate, delete existing per source, then upsert.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A success result when the sync completes without error.</returns>
	public async Task<Result> Handle(SyncSpeedCamerasCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		// Fetch from all providers in parallel
		var fetchTasks = _providers.Select(p => p.FetchAsync(country, ct)).ToList();
		await Task.WhenAll(fetchTasks);

		var allRecords = fetchTasks.SelectMany(t => t.Result).ToList();

		// Deduplicate: cameras within 50 m + same direction from different sources
		var deduplicated = DeduplicateCameras(allRecords);

		// Delete existing data for this country then reload
		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		// Upsert all deduplicated cameras
		foreach (var record in deduplicated)
		{
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, record.Source, country, ct);

			if (existing is null)
			{
				var cameraResult = SpeedCamera.Create(record.ExternalId, record.Source, country, record.Latitude, record.Longitude, record.SpeedLimitKmh, record.Direction);
				if (cameraResult.IsFailure)
					continue;
				await _repository.AddAsync(cameraResult.Value, ct);
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude, record.SpeedLimitKmh, record.Direction);
				await _repository.UpdateAsync(existing, ct);
			}
		}

		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Clusters camera records from different sources. If two cameras from different
	/// sources are within 50 metres and have the same (or Unknown) direction, they are
	/// considered the same physical camera. The OSM record wins when available;
	/// otherwise the first record in the cluster is kept.
	/// </summary>
	/// <param name="records">All camera records fetched from all providers.</param>
	/// <returns>A deduplicated list of camera records.</returns>
	private static List<SpeedCameraRecord> DeduplicateCameras(List<SpeedCameraRecord> records)
	{
		var result = new List<SpeedCameraRecord>();
		var used = new HashSet<int>();

		for (var i = 0; i < records.Count; i++)
		{
			if (used.Contains(i))
				continue;

			var cluster = new List<int> { i };

			for (var j = i + 1; j < records.Count; j++)
			{
				if (used.Contains(j))
					continue;

				var a = records[i];
				var b = records[j];

				var sameDirection = a.Direction == CameraDirection.Unknown || b.Direction == CameraDirection.Unknown || a.Direction == b.Direction;
				if (!sameDirection)
					continue;

				if (HaversineMeters(a.Latitude, a.Longitude, b.Latitude, b.Longitude) <= DeduplicationDistanceMeters)
					cluster.Add(j);
			}

			var winner = cluster.Select(idx => records[idx]).OrderBy(r => r.Source == "osm" ? 0 : 1).First();
			result.Add(winner);

			foreach (var idx in cluster)
				used.Add(idx);
		}

		return result;
	}

	/// <summary>
	/// Computes the Haversine great-circle distance in metres between two WGS84 coordinates.
	/// </summary>
	/// <param name="lat1">Latitude of the first point in degrees.</param>
	/// <param name="lon1">Longitude of the first point in degrees.</param>
	/// <param name="lat2">Latitude of the second point in degrees.</param>
	/// <param name="lon2">Longitude of the second point in degrees.</param>
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