namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Motorcycle-specific costing options sent to Valhalla's /route endpoint.
///              All preference values are floats in the range [0.0, 1.0]:
///              0.0 = strongly avoid, 1.0 = strongly prefer.
///
///              Serialized as snake_case by ValhallaRoutingClient:
///                use_highways, use_tolls, use_trails, use_living_streets, top_speed
///
///              Parameters that do NOT exist in Valhalla motorcycle costing and must NOT
///              be sent (Valhalla silently ignores unknown parameters — no error, no effect):
///                use_hills      — bicycle costing only
///                service_factor — not a valid motorcycle costing option
///
///              To produce hilly / scenic motorcycle routes, insert lateral intermediate
///              waypoints that force Valhalla to route through hilly terrain geographically.
///              See RouteCandidateGeneratorService.GeneratePointToPointVariantsAsync.
/// </summary>
/// <param name="UseHighways">Preference for motorways. 0.0 = avoid, 1.0 = prefer.</param>
/// <param name="UseTolls">Preference for toll roads. 0.0 = avoid, 1.0 = prefer.</param>
/// <param name="UseTrails">Preference for unpaved roads. 0.0 = avoid, 0.5 = neutral, 1.0 = prefer.</param>
/// <param name="UseLivingStreets">Preference for urban residential streets. 0.0 = avoid, 0.6 = allow.</param>
/// <param name="TopSpeed">Optional speed cap in km/h. Null = no cap.</param>
public sealed record ValhallaMotorcycleOptions(
	double UseHighways,
	double UseTolls,
	double UseTrails,
	double UseLivingStreets,
	double? TopSpeed = null);