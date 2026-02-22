namespace Curvia.Persistence.EntityFrameworkCore.Constants;

internal static class DbTableNames
{
	#region Routing
	public const string RoutePlans = nameof(RoutePlans);
	public const string RoutePlanWaypoints = nameof(RoutePlanWaypoints);
	public const string Routes = nameof(Routes);
	public const string RouteSegments = nameof(RouteSegments);
	#endregion

	#region Users
	public const string AppUsers = nameof(AppUsers);
	public const string Motorcycles = nameof(Motorcycles);
	#endregion

	#region Saved Routes
	public const string SavedRoutes = nameof(SavedRoutes);
	public const string RouteReviews = nameof(RouteReviews);
	#endregion

	#region Motorcycle Catalog
	public const string MotorcycleMakers = nameof(MotorcycleMakers);
	public const string MotorcycleModels = nameof(MotorcycleModels);
	#endregion
}