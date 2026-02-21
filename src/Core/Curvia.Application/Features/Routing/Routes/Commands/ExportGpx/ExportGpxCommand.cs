using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Routing.Routes.Commands.ExportGpx;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command to export a computed route to GPX 1.1 format.
///              Stateless — operates on the polyline data returned by <see cref="GenerateRouteCommand"/>
///              without requiring a database lookup.
///              Supports single-track routes (point-to-point and SameRoute loops) and
///              dual-track routes (DifferentRoute loops with outbound + return).
/// </summary>
public sealed class ExportGpxCommand : ICommand<ExportGpxResponse>
{
	#region Route identity

	/// <summary>
	/// Human-readable name embedded in GPX metadata.
	/// Defaults to "Curvia Route" when not supplied.
	/// </summary>
	public string? RouteName { get; init; }

	#endregion

	#region Outbound track (always required)

	/// <summary>
	/// Ordered list of [latitude, longitude] pairs forming the outbound route polyline.
	/// Matches the <c>Polyline</c> field in <see cref="GenerateRouteResponse"/>.
	/// Minimum 2 points required.
	/// </summary>
	public IReadOnlyList<double[]> Polyline { get; init; } = Array.Empty<double[]>();

	/// <summary>Total outbound route distance in meters.</summary>
	public double DistanceMeters { get; init; }

	/// <summary>Estimated outbound riding duration in minutes.</summary>
	public int EstimatedDurationMinutes { get; init; }

	/// <summary>Fun rating (1–5) of the outbound route.</summary>
	public int FunRating { get; init; }

	/// <summary>Raw fun score of the outbound route.</summary>
	public double FunScore { get; init; }

	#endregion

	#region Return leg (DifferentRoute loops only — optional)

	/// <summary>
	/// Ordered list of [latitude, longitude] pairs forming the return leg polyline.
	/// Matches the <c>ReturnLeg.Polyline</c> field in <see cref="GenerateRouteResponse"/>.
	/// Null or empty = single-track GPX export (point-to-point / SameRoute loop).
	/// </summary>
	public IReadOnlyList<double[]>? ReturnLegPolyline { get; init; }

	/// <summary>Return leg distance in meters. 0 if not present.</summary>
	public double ReturnLegDistanceMeters { get; init; }

	/// <summary>Return leg estimated duration in minutes. 0 if not present.</summary>
	public int ReturnLegDurationMinutes { get; init; }

	/// <summary>Fun rating (1–5) of the return leg. 0 if not present.</summary>
	public int ReturnLegFunRating { get; init; }

	#endregion
}