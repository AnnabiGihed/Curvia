namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Models;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a closed GeoJSON-style polygon ring passed to Valhalla's
///              <c>exclude_polygons</c> parameter to force the router away from a
///              previously computed corridor (used for DifferentRoute loop return legs
///              and round-trip return legs).
///
///              Valhalla wire format:
///              <code>
///              "exclude_polygons": [
///                [[lon0,lat0],[lon1,lat1],[lon2,lat2],[lon0,lat0]]
///              ]
///              </code>
///              Each ring is a closed list of [longitude, latitude] pairs.
///              The first and last pair must be identical to close the ring.
///              Coordinates are WGS-84 decimal degrees.
/// </summary>
/// <param name="Ring">
/// Ordered list of [longitude, latitude] pairs forming the closed polygon ring.
/// The last element must equal the first element to satisfy GeoJSON closure.
/// Minimum 4 points (3 unique vertices + closing point).
/// </param>
public sealed record ValhallaExcludePolygon(IReadOnlyList<double[]> Ring);