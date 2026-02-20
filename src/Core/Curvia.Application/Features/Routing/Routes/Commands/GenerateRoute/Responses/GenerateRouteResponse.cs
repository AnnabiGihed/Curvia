namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : DTO returned by <see cref="GenerateRouteCommandHandler"/> after a successful route computation.
///              Contains geometry, statistics, fun rating and optionally a return leg for DifferentRoute loops.
/// </summary>
public sealed record GenerateRouteResponse
{
	#region Geometry
	/// <summary>Geographic bounding box enclosing the full route.</summary>
	public BoundingBoxDto BoundingBox { get; init; } = default!;

	/// <summary>
	/// Ordered list of [latitude, longitude] coordinate pairs forming the route polyline.
	/// </summary>
	public IReadOnlyList<double[]> Polyline { get; init; } = Array.Empty<double[]>();
	#endregion

	#region Statistics
	/// <summary>
	/// Human-friendly fun rating from 1 to 5 stars.
	/// Derived from the normalised fun score against variant benchmarks.
	/// Intended for direct display in the mobile UI.
	/// </summary>
	public int FunRating { get; init; }

	/// <summary>
	/// Raw fun score computed by the scoring service.
	/// Higher is better. Useful for internal analytics.
	/// </summary>
	public double FunScore { get; init; }

	/// <summary>Total route distance in meters.</summary>
	public double DistanceMeters { get; init; }

	/// <summary>Estimated riding duration in minutes (rounded to nearest minute).</summary>
	public int EstimatedDurationMinutes { get; init; }
	#endregion

	#region Identification
	/// <summary>Unique identifier of the generated route aggregate.</summary>
	public Guid RouteId { get; init; }

	/// <summary>
	/// Valhalla variant that produced this route (e.g. "balanced", "more-scenic", "no-tolls").
	/// Useful for debugging and analytics.
	/// </summary>
	public string VariantName { get; init; } = string.Empty;
	#endregion

	#region Loop — Return Leg (optional)
	/// <summary>
	/// Present only when the route was generated with
	/// <see cref="ReturnStrategy.DifferentRoute"/> on the <see cref="LoopSpec"/>.
	/// Contains the return leg geometry and statistics.
	/// Null for point-to-point routes and <see cref="ReturnStrategy.SameRoute"/> loops.
	/// </summary>
	public ReturnLegDto? ReturnLeg { get; init; }
	#endregion
}