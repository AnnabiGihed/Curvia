namespace Curvia.Domain.Features.Routing.RoutePlans.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Named riding presets that map to predefined scoring weight configurations.
///              Allows the mobile app to offer one-tap route personality selection
///              without requiring the user to manually tune individual weights.
///              When <see cref="Custom"/> is selected, the caller must supply explicit
///              FunFactor, CurvesWeight, ElevationWeight, and SceneryWeight values.
/// </summary>
public enum RoutePreset
{
	/// <summary>
	/// Maximises twisty, flowing roads. High curves weight, low elevation and scenery.
	/// Ideal for sport/naked riders who love technical corners.
	/// Curves=0.80 | Elevation=0.10 | Scenery=0.10 | FunFactor=9
	/// </summary>
	Twisty = 0,

	/// <summary>
	/// Prioritises mountain passes and panoramic roads.
	/// High elevation and scenery weights, lower curves weight.
	/// Ideal for touring/adventure riders who want dramatic landscapes.
	/// Curves=0.20 | Elevation=0.40 | Scenery=0.40 | FunFactor=7
	/// </summary>
	Panoramic = 1,

	/// <summary>
	/// Balanced mix of curves, elevation changes and scenic surroundings.
	/// Good default for most riders who want a varied, enjoyable ride.
	/// Curves=0.50 | Elevation=0.25 | Scenery=0.25 | FunFactor=8
	/// </summary>
	Balanced = 2,

	/// <summary>
	/// User-defined weights. The values supplied in the request are used directly.
	/// FunFactor, CurvesWeight, ElevationWeight, and SceneryWeight are all required.
	/// </summary>
	Custom = 3,

	/// <summary>
	/// Maximises elevation and mountain relief. Ideal for riders who love passes and climbs.
	/// Curves=0.40 | Elevation=1.00 | Scenery=0.40 | FunFactor=8
	/// </summary>
	Hilly = 4,

	/// <summary>
	/// Maximises scenic surroundings — forests, lakes, ridges. Ideal for touring riders.
	/// Curves=0.30 | Elevation=0.40 | Scenery=1.00 | FunFactor=7
	/// </summary>
	Scenic = 5,

	/// <summary>
	/// Maximises enjoyment across all dimensions simultaneously. Every dial turned up.
	/// Curves=1.00 | Elevation=0.80 | Scenery=0.60 | FunFactor=10
	/// </summary>
	MaxFun = 6
}