namespace Curvia.Application.Features.Routing.Routes.Contracts;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Valhalla motorcycle costing options sent in the route request body.
///              All float properties are in [0, 1] range (0 = strongly avoid, 1 = strongly prefer).
///              Serialized as snake_case by ValhallaRoutingClient (use_highways, use_tolls, top_speed).
/// </summary>
public sealed record ValhallaMotorcycleOptions(
	/// <summary>0 = avoid highways entirely, 1 = strongly prefer highways.</summary>
	double UseHighways,

	/// <summary>0 = avoid tolls entirely, 1 = allow and prefer toll roads.</summary>
	double UseTolls,

	/// <summary>Optional maximum speed cap in km/h. Null = no cap.</summary>
	double? TopSpeed
);