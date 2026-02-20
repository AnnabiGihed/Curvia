namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Named riding presets that map to predefined scoring weight configurations.
///              Allows the mobile app to offer one-tap route personality selection
///              without requiring the user to manually tune individual weights.
///              When <see cref="Custom"/> is selected, the weights provided in the request are used as-is.
/// </summary>
public enum RoutePreset
{
	/// <summary>
	/// Maximises twisty, flowing roads. High curves weight, low elevation and scenery.
	/// Ideal for sport/naked riders who love technical corners.
	/// Weights: Curves=0.80, Elevation=0.10, Scenery=0.10 | FunFactor=9
	/// </summary>
	Twisty = 0,

	/// <summary>
	/// Prioritises mountain passes and panoramic roads.
	/// High elevation and scenery weights, lower curves weight.
	/// Ideal for touring / adventure riders who want dramatic landscapes.
	/// Weights: Curves=0.20, Elevation=0.40, Scenery=0.40 | FunFactor=7
	/// </summary>
	Panoramic = 1,

	/// <summary>
	/// Balanced mix of curves, elevation changes and scenic surroundings.
	/// Good default for most riders who want a varied, enjoyable ride.
	/// Weights: Curves=0.50, Elevation=0.25, Scenery=0.25 | FunFactor=8
	/// </summary>
	Balanced = 2,

	/// <summary>
	/// User-defined weights. The values supplied in the request are used directly.
	/// Use this when the rider wants full control over the scoring profile.
	/// </summary>
	Custom = 3
}