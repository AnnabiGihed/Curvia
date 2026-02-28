using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routes.Commands.ExportGpx.Responses;

namespace Curvia.Application.Features.Routes.Commands.ExportGpx;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command to export a computed route to GPX 1.1 format.
///              Stateless — operates on the polyline data returned by GenerateRoute
///              without requiring a database lookup.
///
///              The structure intentionally mirrors GenerateRouteResponse so the route
///              response body can be forwarded directly to this endpoint without any
///              client-side field mapping. In particular, ReturnLeg is a nested object
///              matching ReturnLegDto, not a flat set of returnLeg* properties.
/// </summary>
public sealed class ExportGpxCommand : ICommand<ExportGpxResponse>
{
	#region Route identity
	/// <summary>
	/// Human-readable name embedded in GPX metadata and used as the download filename.
	/// Defaults to "Curvia Route" when not supplied.
	/// </summary>
	public string? RouteName { get; init; }
	#endregion

	#region Outbound track (always required)
	/// <summary>Fun rating (1–5) of the outbound route.</summary>
	public int FunRating { get; init; }

	/// <summary>Raw fun score of the outbound route.</summary>
	public double FunScore { get; init; }

	/// <summary>Total outbound route distance in meters.</summary>
	public double DistanceMeters { get; init; }

	/// <summary>Estimated outbound riding duration in minutes.</summary>
	public int EstimatedDurationMinutes { get; init; }

	/// <summary>
	/// Ordered list of [latitude, longitude] pairs forming the outbound route polyline.
	/// Matches the top-level <c>polyline</c> field in GenerateRouteResponse.
	/// Minimum 2 points required.
	/// </summary>
	public IReadOnlyList<double[]> Polyline { get; init; } = Array.Empty<double[]>();
	#endregion

	#region Return leg — nested object matching ReturnLegDto (optional)
	/// <summary>
	/// Return leg data. Matches the <c>returnLeg</c> field in GenerateRouteResponse exactly,
	/// so the full route response can be forwarded to this endpoint without client-side mapping.
	/// Null for one-way point-to-point routes and SameRoute loops.
	/// </summary>
	public ReturnLegExportDto? ReturnLeg { get; init; }
	#endregion
}