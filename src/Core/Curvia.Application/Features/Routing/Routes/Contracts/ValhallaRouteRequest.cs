namespace Curvia.Application.Features.Routing.Routes.Contracts;

public sealed record ValhallaRouteRequest(IReadOnlyList<ValhallaLocation> Locations, string Costing, ValhallaCostingOptions CostingOptions, ValhallaDirectionsOptions DirectionsOptions);
