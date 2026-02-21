namespace Curvia.Application.Features.Routes.Commands.ExportGpx.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Return leg subset used by ExportGpxCommand.
///              Property names match ReturnLegDto exactly so the JSON from GenerateRoute
///              deserializes into this type without any transformation.
/// </summary>
public sealed class ReturnLegExportDto
{
	/// <summary>
	/// Ordered list of [latitude, longitude] pairs forming the return leg polyline.
	/// Matches <c>returnLeg.polyline</c> in GenerateRouteResponse.
	/// </summary>
	public IReadOnlyList<double[]> Polyline { get; init; } = Array.Empty<double[]>();

	/// <summary>Return leg distance in meters.</summary>
	public double DistanceMeters { get; init; }

	/// <summary>Return leg estimated duration in minutes.</summary>
	public int EstimatedDurationMinutes { get; init; }

	/// <summary>Fun rating (1–5) of the return leg.</summary>
	public int FunRating { get; init; }

	/// <summary>Raw fun score of the return leg.</summary>
	public double FunScore { get; init; }
}