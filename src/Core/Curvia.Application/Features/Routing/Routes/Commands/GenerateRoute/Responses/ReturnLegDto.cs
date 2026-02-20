namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Return leg data embedded in <see cref="GenerateRouteResponse"/> for DifferentRoute loops.
///              Contains the geometry and statistics of the return portion of the ride.
/// </summary>
public sealed record ReturnLegDto
{
	/// <summary>Unique identifier of the return route aggregate.</summary>
	public Guid RouteId { get; init; }

	/// <summary>Fun rating (1–5) of the return leg.</summary>
	public int FunRating { get; init; }

	/// <summary>Fun score of the return leg.</summary>
	public double FunScore { get; init; }

	/// <summary>Total return leg distance in meters.</summary>
	public double DistanceMeters { get; init; }

	/// <summary>Estimated return leg duration in minutes.</summary>
	public int EstimatedDurationMinutes { get; init; }

	/// <summary>Bounding box enclosing the return leg.</summary>
	public BoundingBoxDto BoundingBox { get; init; } = default!;

	/// <summary>Ordered polyline coordinates [lat, lon] for the return leg.</summary>
	public IReadOnlyList<double[]> Polyline { get; init; } = Array.Empty<double[]>();
}