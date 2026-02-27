using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Features.Awareness.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

internal sealed class SyncSpeedCamerasCommandHandler : ICommandHandler<SyncSpeedCamerasCommand>
{
	private readonly IEnumerable<ISpeedCameraDataProvider> _providers;
	private readonly ISpeedCameraRepository _repository;

	public SyncSpeedCamerasCommandHandler(
		IEnumerable<ISpeedCameraDataProvider> providers,
		ISpeedCameraRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}

	public async Task<Result> Handle(SyncSpeedCamerasCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		// Fetch from all providers in parallel
		var fetchTasks = _providers.Select(p => p.FetchAsync(country, ct)).ToList();
		await Task.WhenAll(fetchTasks);

		var allRecords = fetchTasks
			.SelectMany(t => t.Result)
			.ToList();

		// Deduplicate: cameras within 50 m + same direction from different sources
		var deduplicated = DeduplicateCameras(allRecords);

		// Delete existing data for this country then reload
		// Using per-source delete so we don't destroy data from other sources
		// if a single source fetch fails. Sources are isolated.
		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
		{
			await _repository.DeleteBySourceAsync(providerName, country, ct);
		}

		// Upsert all deduplicated cameras
		foreach (var record in deduplicated)
		{
			var existing = await _repository.FindByExternalIdAsync(
				record.ExternalId, record.Source, country, ct);

			if (existing is null)
			{
				var camera = SpeedCamera.Create(
					record.ExternalId,
					record.Source,
					country,
					record.Latitude,
					record.Longitude,
					record.SpeedLimitKmh,
					record.Direction);
				await _repository.AddAsync(camera, ct);
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude,
					record.SpeedLimitKmh, record.Direction);
				await _repository.UpdateAsync(existing, ct);
			}
		}

		return Result.Success();
	}

	/// <summary>
	/// Clusters camera records from different sources. If two cameras from different
	/// sources are within 50 metres and have the same (or Unknown) direction, they are
	/// considered the same physical camera. The OSM record wins when available;
	/// otherwise the first record in the cluster is kept.
	/// </summary>
	private static List<SpeedCameraRecord> DeduplicateCameras(
		List<SpeedCameraRecord> records)
	{
		const double DeduplicationRadiusMeters = 50.0;
		var result = new List<SpeedCameraRecord>();
		var visited = new HashSet<int>();

		for (var i = 0; i < records.Count; i++)
		{
			if (visited.Contains(i)) continue;

			var cluster = new List<int> { i };
			for (var j = i + 1; j < records.Count; j++)
			{
				if (visited.Contains(j)) continue;
				if (HaversineMeters(records[i], records[j]) <= DeduplicationRadiusMeters
					&& DirectionsCompatible(records[i].Direction, records[j].Direction))
				{
					cluster.Add(j);
					visited.Add(j);
				}
			}
			visited.Add(i);

			// Prefer OSM record if present in the cluster
			var winner = cluster
				.Select(idx => records[idx])
				.FirstOrDefault(r => r.ExternalId.StartsWith("osm:", StringComparison.OrdinalIgnoreCase))
				?? records[cluster[0]];

			result.Add(winner);
		}

		return result;
	}

	private static bool DirectionsCompatible(CameraDirection a, CameraDirection b)
		=> a == b
		   || a == CameraDirection.Unknown
		   || b == CameraDirection.Unknown;

	private static double HaversineMeters(SpeedCameraRecord a, SpeedCameraRecord b)
	{
		const double R = 6_371_000.0;
		var lat1 = a.Latitude * Math.PI / 180.0;
		var lat2 = b.Latitude * Math.PI / 180.0;
		var dLat = (b.Latitude - a.Latitude) * Math.PI / 180.0;
		var dLon = (b.Longitude - a.Longitude) * Math.PI / 180.0;
		var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
				 + Math.Cos(lat1) * Math.Cos(lat2)
				 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return R * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
	}
}