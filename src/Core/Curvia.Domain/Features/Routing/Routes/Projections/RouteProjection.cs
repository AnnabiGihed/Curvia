using Curvia.Domain.Features.Routing.Routes.Entities;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Routing.Routes.Projections;

public sealed class RouteProjection : ProjectionRoot<RouteProjectionId>
{
	#region Properties
	public RouteStats Stats { get; set; } = default!;
	public Polyline Geometry { get; set; } = default!;
	public BoundingBox BoundingBox { get; set; } = default!;
	public GraphVersionId GraphVersionId { get; set; } = default!;
	public List<RouteSegment> Segments { get; set; } = new();
	#endregion
}
