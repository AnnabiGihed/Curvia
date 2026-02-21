using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.HeightClient;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Request body for Valhalla's /height endpoint.
///              Accepts an array of lat/lon shape points and an optional resample distance.
///              Valhalla returns an elevation value in meters for each input point.
/// </summary>
/// <param name="Shape">Ordered list of coordinates along the route.</param>
/// <param name="Range">
/// When true, Valhalla also returns cumulative distance for each point.
/// We only need heights so this stays false.
/// </param>
/// <param name="ResampleDistance">
/// Optional resampling interval in meters.
/// Valhalla will interpolate additional points at this spacing.
/// Null = use the shape points as-is.
/// </param>
public sealed record ValhallaHeightRequest(IReadOnlyList<ValhallaLocation> Shape, bool Range = false, double? ResampleDistance = null);