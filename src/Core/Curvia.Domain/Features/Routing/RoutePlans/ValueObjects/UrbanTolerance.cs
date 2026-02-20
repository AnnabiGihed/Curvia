namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines the rider's tolerance for urban roads and town streets during route generation.
///              Maps directly to Valhalla's <c>use_living_streets</c> costing parameter.
/// </summary>
public enum UrbanTolerance
{
	/// <summary>
	/// Actively avoids all urban streets, town centres and living streets.
	/// Best for: pure countryside rides.
	/// Valhalla: use_living_streets = 0.0
	/// </summary>
	None = 0,

	/// <summary>
	/// Passes through towns only when there is no alternative route.
	/// Strongly prefers rural and secondary roads.
	/// Best for: mostly rural rides with occasional town crossings.
	/// Valhalla: use_living_streets = 0.2
	/// </summary>
	PassThrough = 1,

	/// <summary>
	/// No restriction on urban roads. Default behaviour.
	/// Best for: mixed rides that may include urban sections.
	/// Valhalla: use_living_streets = 0.6
	/// </summary>
	Allowed = 2
}