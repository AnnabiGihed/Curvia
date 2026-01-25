using System.Globalization;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.Errors;

internal static class RoutingPlansErrors
{
	#region RoutePlan

	public static Error WaypointsTooMany(int count)
		=> new(
			"Routing.RoutePlan.WaypointsTooMany",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_RoutePlan_WaypointsTooMany, count));

	public static Error EndOrLoopRequired()
		=> new(
			"Routing.RoutePlan.EndOrLoopRequired",
			Resource.Routing_RoutePlan_EndOrLoopRequired);

	public static Error EndAndLoopNotAllowed()
		=> new(
			"Routing.RoutePlan.EndAndLoopNotAllowed",
			Resource.Routing_RoutePlan_EndAndLoopNotAllowed);

	#endregion
}