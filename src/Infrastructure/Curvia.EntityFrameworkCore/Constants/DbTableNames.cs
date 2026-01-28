namespace Curvia.Persistence.EntityFrameworkCore.Constants;

internal static class DbTableNames
{
	#region Routing
	#region Route Plans
	public const string RoutePlans = nameof(RoutePlans);
	public const string RoutePlanWaypoints = nameof(RoutePlanWaypoints);
	#endregion

	#region Route
	public const string Routes = nameof(Routes);
	public const string RouteSegments = "RouteSegments";
	#endregion
	#endregion
}
