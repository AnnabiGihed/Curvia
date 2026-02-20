namespace Curvia.Application.Features.Routing.Routes.Contracts;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Request body sent to Valhalla's /route endpoint.
///              Serialized as snake_case by ValhallaRoutingClient
///              (locations, costing, costing_options, directions_options).
/// </summary>
public sealed record ValhallaRouteRequest(IReadOnlyList<ValhallaLocation> Locations, string Costing, ValhallaCostingOptions CostingOptions, ValhallaDirectionsOptions DirectionsOptions);