using Curvia.Application.Features.Awareness.Responses;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Pivot.Framework.Domain.Shared;

namespace Curvia.Application.Features.Awareness.Queries.GetAwarenessForArea;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetAwarenessForAreaQuery"/>.
///
///              Pipeline:
///                1. Build and validate BoundingBox VO.
///                2. Enforce max area cap (4 square degrees ≈ 400 km × 400 km).
///                3. Fan out to requested repositories in parallel.
///                4. Assemble AwarenessResultDto.
/// </summary>
internal sealed class GetAwarenessForAreaQueryHandler : IQueryHandler<GetAwarenessForAreaQuery, AwarenessResultDto>
{
	#region Constants
	/// <summary>
	/// Maximum query area in square degrees.
	/// 4° × 4° ≈ 444 km × 444 km — large enough for any ride corridor,
	/// small enough to prevent full-continent scans via the public endpoint.
	/// </summary>
	private const double MaxAreaDegrees = 16.0;
	#endregion

	#region Dependencies
	private readonly ISpeedCameraRepository _speedCameraRepo;
	private readonly IHazardRepository _hazardRepo;
	private readonly IRoadWorkRepository _roadWorkRepo;
	private readonly IIncidentRepository _incidentRepo;
	#endregion

	#region Constructor
	public GetAwarenessForAreaQueryHandler(
		ISpeedCameraRepository speedCameraRepo,
		IHazardRepository hazardRepo,
		IRoadWorkRepository roadWorkRepo,
		IIncidentRepository incidentRepo)
	{
		_speedCameraRepo = speedCameraRepo ?? throw new ArgumentNullException(nameof(speedCameraRepo));
		_hazardRepo = hazardRepo ?? throw new ArgumentNullException(nameof(hazardRepo));
		_roadWorkRepo = roadWorkRepo ?? throw new ArgumentNullException(nameof(roadWorkRepo));
		_incidentRepo = incidentRepo ?? throw new ArgumentNullException(nameof(incidentRepo));
	}
	#endregion

	#region IQueryHandler
	public async Task<Result<AwarenessResultDto>> Handle(GetAwarenessForAreaQuery query, CancellationToken cancellationToken)
	{
		var bboxResult = BoundingBox.Create(query.MinLat, query.MinLon, query.MaxLat, query.MaxLon);
		if (bboxResult.IsFailure)
			return Result.Failure<AwarenessResultDto>(bboxResult.Error);

		var bbox = bboxResult.Value;

		if (bbox.ApproxAreaDegrees > MaxAreaDegrees)
			return Result.Failure<AwarenessResultDto>(new Error(
				"Awareness.AreaTooLarge",
				$"The requested area is too large. Maximum is {MaxAreaDegrees}°² "
			  + $"(≈ 400 km × 400 km). Your area is {bbox.ApproxAreaDegrees:F2}°²."));

		return await FetchAndAssembleAsync(bbox, query.Types, cancellationToken);
	}
	#endregion

	#region Private helpers
	private async Task<Result<AwarenessResultDto>> FetchAndAssembleAsync(BoundingBox bbox, AwarenessItemType[] types, CancellationToken ct)
	{
		var typesSet = new HashSet<AwarenessItemType>(types);

		// Fan out only to requested repositories — avoids unnecessary DB calls
		// when mobile requests a single category (e.g. cameras-only overlay).
		var camerasTask = typesSet.Contains(AwarenessItemType.SpeedCamera)
			? _speedCameraRepo.GetInAreaAsync(bbox, ct)
			: Task.FromResult<IReadOnlyList<SpeedCamera>>(Array.Empty<SpeedCamera>());

		var hazardsTask = typesSet.Contains(AwarenessItemType.Hazard)
			? _hazardRepo.GetInAreaAsync(bbox, ct)
			: Task.FromResult<IReadOnlyList<Hazard>>(Array.Empty<Hazard>());

		var roadWorksTask = typesSet.Contains(AwarenessItemType.RoadWork)
			? _roadWorkRepo.GetInAreaAsync(bbox, ct)
			: Task.FromResult<IReadOnlyList<RoadWork>>(Array.Empty<RoadWork>());

		var incidentsTask = typesSet.Contains(AwarenessItemType.Incident)
			? _incidentRepo.GetInAreaAsync(bbox, ct)
			: Task.FromResult<IReadOnlyList<Incident>>(Array.Empty<Incident>());

		await Task.WhenAll(camerasTask, hazardsTask, roadWorksTask, incidentsTask);

		var cameras = camerasTask.Result;
		var hazards = hazardsTask.Result;
		var roadWorks = roadWorksTask.Result;
		var incidents = incidentsTask.Result;

		var freshnessUtc = ComputeFreshness(cameras, hazards, roadWorks, incidents);

		var dto = new AwarenessResultDto(
			SpeedCameras: cameras.Select(c => new SpeedCameraDto(
				c.Id.Value, c.Position.Latitude, c.Position.Longitude,
				c.SpeedLimit.Kmh, c.Direction, c.Source, c.CountryCode)).ToList(),
			Hazards: hazards.Select(h => new HazardDto(
				h.Id.Value, h.Position.Latitude, h.Position.Longitude,
				h.HazardType, h.Description, h.Source, h.CountryCode)).ToList(),
			RoadWorks: roadWorks.Select(r => new RoadWorkDto(
				r.Id.Value, r.Position.Latitude, r.Position.Longitude,
				r.Title, r.Description, r.ValidFromUtc, r.ValidUntilUtc,
				r.Source, r.CountryCode)).ToList(),
			Incidents: incidents.Select(i => new IncidentDto(
				i.Id.Value, i.Position.Latitude, i.Position.Longitude,
				i.IncidentType, i.Severity, i.Description,
				i.ValidFromUtc, i.ValidUntilUtc, i.Source, i.CountryCode)).ToList(),
			DataFreshnessUtc: freshnessUtc);

		return Result.Success(dto);
	}

	/// <summary>
	/// Returns the oldest LastSyncedUtc across all returned items.
	/// Falls back to UtcNow when all lists are empty (no data = freshness is irrelevant).
	/// </summary>
	private static DateTime ComputeFreshness(IReadOnlyList<SpeedCamera> cameras, IReadOnlyList<Hazard> hazards, IReadOnlyList<RoadWork> roadWorks, IReadOnlyList<Incident> incidents)
	{
		var candidates = Enumerable.Empty<DateTime>()
			.Concat(cameras.Select(c => c.LastSyncedUtc))
			.Concat(hazards.Select(h => h.LastSyncedUtc))
			.Concat(roadWorks.Select(r => r.LastSyncedUtc))
			.Concat(incidents.Select(i => i.LastSyncedUtc));

		return candidates.Any() ? candidates.Min() : DateTime.UtcNow;
	}
	#endregion
}