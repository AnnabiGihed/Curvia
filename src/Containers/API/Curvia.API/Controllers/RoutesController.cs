using Curvia.Application.Features.Routing.Routes.Commands.ExportGpx;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Templates.Core.Containers.API.Abstractions;

namespace Curvia.API.Features.Routing.Routes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Exposes the motorcycle fun routing endpoints.
///              POST /api/routes            — generates the best fun route for a ride.
///              POST /api/routes/export/gpx — exports a computed route to GPX 1.1 format.
/// </summary>
[AllowAnonymous]
[Route("api/routes")]
public sealed class RoutesController : ApiController
{
	#region Constructor
	public RoutesController(ISender sender) : base(sender) { }
	#endregion

	#region POST /api/routes

	/// <summary>
	/// Generates the best fun motorcycle route based on rider preferences and constraints.
	/// </summary>
	/// <remarks>
	/// ## Trip Modes
	/// Exactly one of the following must be set:
	/// - **Point-to-point**: set `endLatitude` + `endLongitude`
	/// - **Loop**: set `loopTargetDistanceMeters`
	///
	/// ## Round Trip (point-to-point only)
	/// Set `roundTrip: true` together with `endLatitude`/`endLongitude` to receive both:
	/// - An outbound route from Start → End (best fun roads going out)
	/// - A return leg from End → Start (genuinely different fun roads coming back)
	///
	/// The outbound road corridor is excluded when computing the return, guaranteeing variety.
	/// The `returnLeg` field in the response is populated automatically.
	///
	/// ## Preset Profiles
	/// - **Twisty (0)** — maximises twisty/flowing roads
	/// - **Panoramic (1)** — prioritises mountain passes and scenic landscapes
	/// - **Balanced (2)** — all-round enjoyable ride (default)
	/// - **Custom (3)** — supply `funFactor`, `curvesWeight`, `elevationWeight`, `sceneryWeight`
	///
	/// ## Loop Return Strategies
	/// - **SameRoute (0)** — single circular loop (default)
	/// - **DifferentRoute (1)** — outbound + return on different roads
	///
	/// ## Sample — Point-to-point, one direction (Bruges → Ardennes)
	/// ```json
	/// {
	///   "startLatitude": 51.2093, "startLongitude": 3.2247,
	///   "endLatitude":   50.1415, "endLongitude":   5.8792,
	///   "preset": 0,
	///   "avoidHighways": true, "avoidUnpaved": true,
	///   "urbanTolerance": 1, "maxDetourRatio": 2.5
	/// }
	/// ```
	///
	/// ## Sample — Round trip on different roads (Avenue Louise 216 ↔ Ardennes)
	/// ```json
	/// {
	///   "startLatitude": 50.8252, "startLongitude": 4.3691,
	///   "endLatitude":   50.1415, "endLongitude":   5.8792,
	///   "roundTrip": true,
	///   "preset": 0,
	///   "avoidHighways": true, "avoidUnpaved": true, "urbanTolerance": 0
	/// }
	/// ```
	///
	/// ## Sample — Full-day loop, DifferentRoute return (from Brussels)
	/// ```json
	/// {
	///   "startLatitude": 50.8503, "startLongitude": 4.3517,
	///   "loopTargetDistanceMeters": 250000,
	///   "loopReturnStrategy": 1,
	///   "preset": 1,
	///   "avoidHighways": true, "avoidUnpaved": true,
	///   "urbanTolerance": 0, "maxDurationMinutes": 480
	/// }
	/// ```
	/// </remarks>
	/// <param name="command">Route generation parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <response code="200">Route generated successfully. Returns geometry, stats, fun rating and optional return leg.</response>
	/// <response code="400">Validation failed or no valid route found with the given constraints.</response>
	/// <response code="500">Unexpected error — Valhalla unreachable or internal failure.</response>
	[HttpPost]
	[ProducesResponseType(typeof(GenerateRouteResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> GenerateRoute(
		[FromBody] GenerateRouteCommand command,
		CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);
		return HandleResult(result);
	}

	#endregion

	#region POST /api/routes/export/gpx

	/// <summary>
	/// Exports a computed route to GPX 1.1 format for import into navigation devices and apps.
	/// </summary>
	/// <remarks>
	/// Call this endpoint after POST /api/routes. Pass the polyline data from the route response —
	/// no server-side state or route ID is required.
	///
	/// Compatible with: Garmin BaseCamp, TwoNav, Kurviger, OsmAnd, Komoot, Google Maps.
	///
	/// For round trips (outbound + return), include the `returnLegPolyline` fields.
	/// The GPX file will contain two tracks — one per leg.
	///
	/// ## Sample — Round trip export
	/// ```json
	/// {
	///   "routeName": "Avenue Louise ↔ Ardennes",
	///   "polyline": [[50.8252, 4.3691], [50.5000, 5.1000], [50.1415, 5.8792]],
	///   "distanceMeters": 155000,
	///   "estimatedDurationMinutes": 140,
	///   "funRating": 4,
	///   "funScore": 1.85,
	///   "returnLegPolyline": [[50.1415, 5.8792], [50.6000, 4.9000], [50.8252, 4.3691]],
	///   "returnLegDistanceMeters": 162000,
	///   "returnLegDurationMinutes": 148,
	///   "returnLegFunRating": 4
	/// }
	/// ```
	/// </remarks>
	/// <param name="command">GPX export parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <response code="200">GPX file as <c>application/gpx+xml</c> with Content-Disposition: attachment.</response>
	/// <response code="400">Validation failed — invalid polyline or missing required fields.</response>
	[HttpPost("export/gpx")]
	[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ExportGpx(
		[FromBody] ExportGpxCommand command,
		CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);

		if (result.IsFailure)
			return HandleFailure(result);

		var response = result.Value;
		var bytes = System.Text.Encoding.UTF8.GetBytes(response.GpxContent);

		return File(
			fileContents: bytes,
			contentType: "application/gpx+xml; charset=utf-8",
			fileDownloadName: response.FileName);
	}

	#endregion
}