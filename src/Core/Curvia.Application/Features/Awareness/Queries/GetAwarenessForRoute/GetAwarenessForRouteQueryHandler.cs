using MediatR;
using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Shared.ValueObjects;
using Curvia.Application.Features.Awareness.Responses;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Awareness.Queries.GetAwarenessForArea;

namespace Curvia.Application.Features.Awareness.Queries.GetAwarenessForRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetAwarenessForRouteQuery"/>.
///
///              Pipeline:
///                1. Load the Route aggregate to get its polyline geometry.
///                2. Validate the buffer radius.
///                3. Derive a BoundingBox from the polyline envelope + buffer.
///                4. Delegate to GetAwarenessForAreaQuery via MediatR (reuse handler logic).
///
///              WHY delegate to GetAwarenessForAreaQuery:
///              The area handler already contains the parallel fan-out, mapping, and
///              freshness logic. Re-dispatching avoids duplication and ensures both
///              query paths stay in sync without sharing a concrete class.
/// </summary>
internal sealed class GetAwarenessForRouteQueryHandler : IQueryHandler<GetAwarenessForRouteQuery, AwarenessResultDto>
{
	#region Constants
	private const double MaxBufferMeters = 2_000.0;
	private const double DefaultBufferMeters = 500.0;
	#endregion

	#region Dependencies
	private readonly ISender _sender;
	private readonly IRouteRepository _routeRepository;
	#endregion

	#region Constructor
	public GetAwarenessForRouteQueryHandler(IRouteRepository routeRepository, ISender sender)
	{
		_sender = sender ?? throw new ArgumentNullException(nameof(sender));
		_routeRepository = routeRepository ?? throw new ArgumentNullException(nameof(routeRepository));
	}
	#endregion

	#region IQueryHandler
	public async Task<Result<AwarenessResultDto>> Handle(GetAwarenessForRouteQuery query, CancellationToken cancellationToken)
	{
		// 1. Validate buffer
		var buffer = query.BufferMeters <= 0 ? DefaultBufferMeters : query.BufferMeters;
		if (buffer > MaxBufferMeters)
			return Result.Failure<AwarenessResultDto>(new Error("Awareness.BufferTooLarge", $"Buffer radius cannot exceed {MaxBufferMeters} metres."));

		// 2. Load route
		var routeId = new RouteId(query.RouteId);
		var route = await _routeRepository.FindByIdAsync(routeId, cancellationToken);
		if (route is null)
			return Result.Failure<AwarenessResultDto>(
				new Error("Route.NotFound", "The route was not found."),
				ResultExceptionType.NotFound);

		// 3. Extract polyline points from route geometry
		var points = route.Geometry.Points
			.Select(p => (p.Latitude, p.Longitude))
			.ToList();

		if (points.Count == 0)
			return Result.Failure<AwarenessResultDto>(
				new Error("Route.EmptyGeometry", "The route has no geometry points."));

		// 4. Derive bounding box from polyline + buffer
		var bboxResult = BoundingBox.CreateFromPolylineWithBuffer(points, buffer);
		if (bboxResult.IsFailure)
			return Result.Failure<AwarenessResultDto>(bboxResult.Error);

		var bbox = bboxResult.Value;

		// 5. Delegate to area query (no area cap applied — derived from actual route)
		var areaQuery = new GetAwarenessForAreaQuery(
			MinLat: bbox.South,
			MinLon: bbox.West,
			MaxLat: bbox.North,
			MaxLon: bbox.East,
			Types: query.Types);

		// We bypass the area cap by calling the handler directly via MediatR.
		// The cap in GetAwarenessForAreaQueryHandler is meant to protect the public
		// area endpoint — route corridors are inherently bounded by the route itself.
		return await _sender.Send(areaQuery, cancellationToken);
	}
	#endregion
}