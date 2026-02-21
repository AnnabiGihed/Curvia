namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Motorcycle-specific costing options sent to Valhalla's /route endpoint.
///              All preference values are floats in the range [0.0, 1.0]:
///              0.0 = strongly avoid / strongly penalise
///              1.0 = strongly prefer / no penalty
///
///              Serialized as snake_case by ValhallaRoutingClient:
///                use_highways, use_tolls, use_trails, use_living_streets,
///                use_hills, service_factor, top_speed
///
///              Key insight — use_hills is the most important parameter for fun routing:
///                Valhalla motorcycle costing uses use_hills to weight roads by gradient.
///                High values (0.8–1.0) cause the engine to actively seek hilly terrain,
///                which correlates directly with curvature and scenic quality.
///                Setting use_hills = 1.0 steers routes toward the Ardennes/Meuse valleys
///                instead of flat N-road corridors through central Belgium.
/// </summary>
/// <param name="UseHighways">
/// Preference for motorways and dual carriageways.
/// 0.0 = strongly avoid, 1.0 = strongly prefer.
/// </param>
/// <param name="UseTolls">
/// Preference for toll roads.
/// 0.0 = strongly avoid, 1.0 = strongly prefer.
/// </param>
/// <param name="UseTrails">
/// Preference for unpaved roads, gravel tracks and off-road trails.
/// 0.0 = avoid unpaved, 0.5 = neutral, 1.0 = prefer.
/// </param>
/// <param name="UseLivingStreets">
/// Preference for residential streets within urban areas.
/// Note: does NOT prevent routing on urban arterials (national roads through city centres).
/// For city avoidance, ServiceFactor is more effective.
/// 0.0 = avoid residential streets, 0.6 = allow freely.
/// </param>
/// <param name="UseHills">
/// Preference for hilly and mountainous terrain.
/// This is the primary lever for generating fun routes: high values cause Valhalla
/// to seek roads with elevation change, which naturally produces curvature and scenic quality.
/// 0.0 = prefer flat roads, 1.0 = strongly prefer hilly/mountain roads.
/// Default in Valhalla: 0.5 (neutral). Curvia fun routing uses 0.8–1.0.
/// </param>
/// <param name="ServiceFactor">
/// Penalty multiplier applied to service-class roads (urban driveways, car parks, access roads).
/// Lower values increase the cost of urban service roads, discouraging city-centre routing.
/// Range [0.0, 1.0]. Default 1.0 (no penalty). Use 0.3–0.5 to reduce urban routing.
/// </param>
/// <param name="TopSpeed">
/// Optional speed cap in km/h. Null means no cap is applied.
/// </param>
public sealed record ValhallaMotorcycleOptions(
	double UseHighways,
	double UseTolls,
	double UseTrails,
	double UseLivingStreets,
	double UseHills,
	double ServiceFactor = 1.0,
	double? TopSpeed = null);