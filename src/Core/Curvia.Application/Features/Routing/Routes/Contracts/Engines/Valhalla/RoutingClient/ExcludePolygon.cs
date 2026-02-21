namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : A single exclusion polygon for Valhalla's <c>exclude_polygons</c> parameter.
///              Each ring is an ordered list of [longitude, latitude] coordinate pairs.
///              The ring must be closed (last point equals first point).
/// </summary>
/// <param name="Ring">Closed polygon ring as an array of [longitude, latitude] pairs.</param>
public sealed record ExcludePolygon(double[][] Ring);