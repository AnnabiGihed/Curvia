namespace Curvia.Application.Features.Routing.Routes.Contracts;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents the JSON body sent to Valhalla's /route endpoint.
///              Serialized as snake_case by <see cref="ValhallaRoutingClient"/>
///              (locations, costing, costing_options, directions_options, exclude_polygons).
/// </summary>
/// <param name="Locations">Ordered list of route locations (start, optional waypoints, end).</param>
/// <param name="Costing">Valhalla costing model. Always "motorcycle" for this platform.</param>
/// <param name="CostingOptions">Motorcycle-specific costing parameters.</param>
/// <param name="DirectionsOptions">Output formatting options (units, language).</param>
/// <param name="ExcludePolygons">
/// Optional list of polygons to exclude from routing.
/// Used for DifferentRoute loop return legs to force a different path from the outbound.
/// Null = no exclusions.
/// </param>
public sealed record ValhallaRouteRequest(IReadOnlyList<ValhallaLocation> Locations, string Costing, ValhallaCostingOptions CostingOptions,	ValhallaDirectionsOptions DirectionsOptions, IReadOnlyList<ExcludePolygon>? ExcludePolygons = null);

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : A single exclusion polygon for Valhalla's <c>exclude_polygons</c> parameter.
///              Each ring is an ordered list of [longitude, latitude] coordinate pairs.
///              The ring must be closed (last point equals first point).
/// </summary>
/// <param name="Ring">Closed polygon ring as an array of [longitude, latitude] pairs.</param>
public sealed record ExcludePolygon(double[][] Ring);