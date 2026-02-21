using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

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
/// Optional list of polygon rings to exclude from routing.
/// Each element is a closed ring represented as a list of [longitude, latitude] pairs.
/// Valhalla format: exclude_polygons = [ [[lon,lat],[lon,lat],...], ... ]
/// Used for DifferentRoute loop return legs to force a different path from the outbound.
/// Null = no exclusions.
/// </param>
public sealed record ValhallaRouteRequest(IReadOnlyList<ValhallaLocation> Locations, string Costing, ValhallaCostingOptions CostingOptions,	ValhallaDirectionsOptions DirectionsOptions, IReadOnlyList<double[][]>? ExcludePolygons = null);