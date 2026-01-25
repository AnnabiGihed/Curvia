using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Routing.Routes.Errors;

internal static class RoutesErrors
{
	#region Route

	public static Error RouteRequiresGeometry()
		=> new(
			"Routing.Route.GeometryRequired",
			Resource.Routing_Route_GeometryRequired);

	public static Error RouteSegmentsInvalid()
		=> new(
			"Routing.Route.SegmentsInvalid",
			Resource.Routing_Route_SegmentsInvalid);

	public static Error GraphVersionRequired()
		=> new(
			"Routing.Route.GraphVersionRequired",
			Resource.Routing_Route_GraphVersionRequired);

	#endregion

	#region RouteSegment

	public static Error SegmentGeometryRequired()
		=> new(
			"Routing.RouteSegment.GeometryRequired",
			Resource.Routing_RouteSegment_GeometryRequired);

	#endregion
}