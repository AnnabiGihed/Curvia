namespace Curvia.Application.Features.Routes.Commands.Generate.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Return leg data embedded in <see cref="GenerateRouteResponse"/> for DifferentRoute loops
///              and round-trip point-to-point journeys. Contains the full geometry, bounding box,
///              and statistics of the return portion of the ride, mirroring the outbound structure
///              so the mobile app can render both legs symmetrically.
/// </summary>
public sealed record ReturnLegDto
{
	#region Identification
	/// <summary>Unique identifier of the return route aggregate.</summary>
	public Guid RouteId { get; init; }

	/// <summary>
	/// Valhalla variant that produced this return leg (e.g. "return-right-early", "return-left-mid").
	/// Useful for debugging and analytics.
	/// </summary>
	public string VariantName { get; init; } = string.Empty;
	#endregion

	#region Geometry
	/// <summary>Geographic bounding box enclosing the full return leg.</summary>
	public BoundingBoxDto BoundingBox { get; init; } = default!;

	/// <summary>Ordered list of [latitude, longitude] coordinate pairs forming the return leg polyline.</summary>
	public IReadOnlyList<double[]> Polyline { get; init; } = Array.Empty<double[]>();
	#endregion

	#region Statistics
	/// <summary>Human-friendly fun rating from 1 to 5 stars for the return leg.</summary>
	public int FunRating { get; init; }

	/// <summary>Raw fun score of the return leg. Higher is better.</summary>
	public double FunScore { get; init; }

	/// <summary>Total return leg distance in meters.</summary>
	public double DistanceMeters { get; init; }

	/// <summary>Estimated return leg riding duration in minutes (rounded to nearest minute).</summary>
	public int EstimatedDurationMinutes { get; init; }
	#endregion
}