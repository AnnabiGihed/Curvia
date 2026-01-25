using Curvia.Domain.Features.Routing.Routes.Errors;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Curvia.Domain.Features.Routing.Shared;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Routing.Routes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Entity representing a segment of a route (for chunking, scoring, analytics, and UI overlays).
/// </summary>
public sealed class RouteSegment : Entity<RouteSegmentId>
{
	#region Properties

	public Polyline Geometry { get; private set; } = default!;
	public RouteStats Stats { get; private set; } = default!;

	#endregion

	#region Constructors

	private RouteSegment(RouteSegmentId id) : base(id) { }

	private RouteSegment() { }

	#endregion

	#region Factory

	public static Result<RouteSegment> Create(Polyline geometry, RouteStats stats)
	{
		if (geometry is null)
			return Result.Failure<RouteSegment>(RoutesErrors.SegmentGeometryRequired());

		if (stats is null)
			return Result.Failure<RouteSegment>(RoutingErrors.NullValue(nameof(stats)));

		var segment = new RouteSegment(RouteSegmentId.New())
		{
			Geometry = geometry,
			Stats = stats
		};

		return Result.Success(segment);
	}

	#endregion
}
