using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Routing.Shared;
using Curvia.Domain.Features.Routing.Routes.Errors;
using Curvia.Domain.Features.Routing.Routes.Entities;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Domain.Features.Routing.Routes.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Aggregate representing a computed route based on a RoutePlan and a given graph version.
///              It is the consistency boundary for computed geometry + segments + statistics.
/// </summary>
public sealed class Route : AggregateRoot<RouteId>
{
	#region Fields
	private readonly List<RouteSegment> _segments = new();
	#endregion

	#region Properties
	public RouteStats Stats { get; private set; } = default!;
	public Polyline Geometry { get; private set; } = default!;
	public BoundingBox BoundingBox { get; private set; } = default!;
	public RoutePlanId RoutePlanId { get; private set; } = default!;
	public GraphVersionId GraphVersionId { get; private set; } = default!;
	public IReadOnlyCollection<RouteSegment> Segments => _segments.AsReadOnly();
	#endregion

	#region Constructors

	private Route(RouteId id) : base(id) { }

	private Route() { }

	#endregion

	#region Factory

	public static Result<Route> Create(RoutePlanId routePlanId,	string graphVersionId, Polyline geometry, BoundingBox boundingBox, RouteStats stats, IReadOnlyCollection<RouteSegment> segments)
	{
		var gv = GraphVersionId.Create(graphVersionId);
		if (gv.IsFailure)
			return Result.Failure<Route>(gv.Error, gv.ResultExceptionType);

		return Create(routePlanId, gv.Value, geometry, boundingBox, stats, segments);
	}
	public static Result<Route> Create(RoutePlanId routePlanId, GraphVersionId graphVersionId, Polyline geometry, BoundingBox boundingBox, RouteStats stats, IReadOnlyCollection<RouteSegment> segments)
	{
		if (routePlanId is null)
			return Result.Failure<Route>(RoutingErrors.NullValue(nameof(routePlanId)));

		if (graphVersionId is null)
			return Result.Failure<Route>(RoutingErrors.GraphVersionIdRequired());

		if (geometry is null)
			return Result.Failure<Route>(RoutesErrors.RouteRequiresGeometry());

		if (boundingBox is null)
			return Result.Failure<Route>(RoutingErrors.NullValue(nameof(boundingBox)));

		if (stats is null)
			return Result.Failure<Route>(RoutingErrors.NullValue(nameof(stats)));

		if (segments is null || segments.Count == 0)
			return Result.Failure<Route>(RoutesErrors.RouteSegmentsInvalid());

		var route = new Route(RouteId.New())
		{
			RoutePlanId = routePlanId,
			GraphVersionId = graphVersionId,
			Geometry = geometry,
			BoundingBox = boundingBox,
			Stats = stats
		};

		foreach (var s in segments)
		{
			if (s is null)
				return Result.Failure<Route>(RoutingErrors.NullValue(nameof(segments)));

			route._segments.Add(s);
		}

		return Result.Success(route);
	}

	#endregion

	#region Domain Behaviors
	public Result AddSegment(RouteSegment segment)
	{
		if (segment is null)
			return Result.Failure(RoutingErrors.NullValue(nameof(segment)));

		_segments.Add(segment);
		return Result.Success();
	}
	#endregion
}