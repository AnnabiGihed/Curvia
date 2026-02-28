namespace Curvia.Persistence.EntityFrameworkCore.Constants;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Centralizes database table name constants used by EF Core configurations.
///              Uses <see cref="nameof"/> to keep constants aligned with identifier names
///              and reduce the risk of mismatches/typos across configurations.
/// </summary>
internal static class DbTableNames
{
	#region Routing
	/// <summary>
	/// Table name for RoutePlan aggregate root persistence.
	/// </summary>
	public const string RoutePlans = nameof(RoutePlans);

	/// <summary>
	/// Table name for RoutePlan waypoints owned collection persistence.
	/// </summary>
	public const string RoutePlanWaypoints = nameof(RoutePlanWaypoints);

	/// <summary>
	/// Table name for Route aggregate root persistence.
	/// </summary>
	public const string Routes = nameof(Routes);

	/// <summary>
	/// Table name for Route segments owned collection persistence.
	/// </summary>
	public const string RouteSegments = nameof(RouteSegments);
	#endregion

	#region Users
	/// <summary>
	/// Table name for application users persistence.
	/// </summary>
	public const string Users = nameof(Users);

	/// <summary>
	/// Table name for motorcycles persistence.
	/// </summary>
	public const string Motorcycles = nameof(Motorcycles);
	#endregion

	#region Saved Routes
	/// <summary>
	/// Table name for saved routes persistence.
	/// </summary>
	public const string SavedRoutes = nameof(SavedRoutes);

	/// <summary>
	/// Table name for route reviews persistence.
	/// </summary>
	public const string RouteReviews = nameof(RouteReviews);
	#endregion

	#region Motorcycle Catalog
	/// <summary>
	/// Table name for motorcycle makers persistence.
	/// </summary>
	public const string MotorcycleMakers = nameof(MotorcycleMakers);

	/// <summary>
	/// Table name for motorcycle catalog models persistence.
	/// </summary>
	public const string MotorcycleModels = nameof(MotorcycleModels);
	#endregion

	#region Awareness
	public const string SpeedCameras = nameof(SpeedCameras);
	public const string Hazards = nameof(Hazards);
	public const string RoadWorks = nameof(RoadWorks);
	public const string Incidents = nameof(Incidents);
	#endregion
}