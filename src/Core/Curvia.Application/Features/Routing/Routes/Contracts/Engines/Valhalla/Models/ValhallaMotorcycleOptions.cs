namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Motorcycle-specific costing options sent to Valhalla.
///              All preference values are floats in the range [0.0, 1.0]:
///              0.0 = strongly avoid, 1.0 = strongly prefer.
///              Serialized as snake_case by <see cref="ValhallaRoutingClient"/>
///              (use_highways, use_tolls, use_trails, use_living_streets, top_speed).
/// </summary>
/// <param name="UseHighways">
/// Preference for motorways and dual carriageways.
/// 0.0 = avoid, 1.0 = prefer.
/// </param>
/// <param name="UseTolls">
/// Preference for toll roads.
/// 0.0 = avoid, 1.0 = prefer.
/// </param>
/// <param name="UseTrails">
/// Preference for unpaved roads, gravel tracks and off-road trails.
/// 0.0 = avoid unpaved, 1.0 = fully allow.
/// </param>
/// <param name="UseLivingStreets">
/// Preference for urban living streets and town-centre roads.
/// 0.0 = avoid towns, 1.0 = fully allow.
/// </param>
/// <param name="TopSpeed">
/// Optional speed cap in km/h. Null means no cap is applied.
/// </param>
public sealed record ValhallaMotorcycleOptions(double UseHighways, double UseTolls,	double UseTrails, double UseLivingStreets, double? TopSpeed = null);