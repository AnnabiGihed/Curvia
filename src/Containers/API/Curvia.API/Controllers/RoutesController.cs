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

	/// <summary>
	/// Initialises the controller with the MediatR sender.
	/// </summary>
	/// <param name="sender">MediatR request dispatcher.</param>
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
	/// - **Loop**: set `loopTargetDistanceMeters` (route starts and ends at the same location)
	///
	/// ## Preset Profiles
	/// Use `preset` for one-tap ride personality selection:
	/// - **Twisty** — maximises twisty/flowing roads (Curves=0.80)
	/// - **Panoramic** — prioritises mountain passes and scenic landscapes (Elevation+Scenery=0.80)
	/// - **Balanced** — all-round enjoyable ride (default)
	/// - **Custom** — supply `funFactor`, `curvesWeight`, `elevationWeight`, `sceneryWeight` manually
	///
	/// ## Loop Return Strategies
	/// When using loop mode:
	/// - **SameRoute** (default) — single circular loop, same roads outbound and return
	/// - **DifferentRoute** — two separate loops with a guaranteed different return path
	///
	/// ## Sample Request — Point-to-point (Bruges → Ardennes)
	/// ```json
	/// {
	///   "startLatitude": 51.2093,
	///   "startLongitude": 3.2247,
	///   "endLatitude": 50.1415,
	///   "endLongitude": 5.8792,
	///   "preset": 0,
	///   "avoidHighways": true,
	///   "avoidTolls": false,
	///   "avoidUnpaved": true,
	///   "urbanTolerance": 1,
	///   "maxDetourRatio": 2.5
	/// }
	/// ```
	///
	/// ## Sample Request — Full-day loop with DifferentRoute return (from Brussels)
	/// ```json
	/// {
	///   "startLatitude": 50.8503,
	///   "startLongitude": 4.3517,
	///   "loopTargetDistanceMeters": 250000,
	///   "loopReturnStrategy": 1,
	///   "preset": 1,
	///   "avoidHighways": true,
	///   "avoidUnpaved": true,
	///   "urbanTolerance": 0,
	///   "maxDurationMinutes": 480
	/// }
	/// ```
	/// </remarks>
	/// <param name="command">Route generation parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <response code="200">Route generated successfully. Returns route geometry, stats and fun rating.</response>
	/// <response code="400">Validation failed or no valid route could be found with the given constraints.</response>
	/// <response code="500">Unexpected error — Valhalla unreachable or internal failure.</response>
	[HttpPost]
	[ProducesResponseType(typeof(GenerateRouteResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> GenerateRoute([FromBody] GenerateRouteCommand command, CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);
		return HandleResult(result);
	}

	#endregion

	#region POST /api/routes/export/gpx

	/// <summary>
	/// Exports a computed route to GPX 1.1 format, ready for import into navigation devices
	/// and apps (Garmin, TwoNav, Kurviger, OsmAnd, Komoot, Google Maps, etc.).
	/// </summary>
	/// <remarks>
	/// ## Usage
	/// Call this endpoint after <c>POST /api/routes</c>. Pass the polyline data from the
	/// route response directly — no route ID or server-side state is required.
	///
	/// ## GPX Structure
	/// The returned file contains:
	/// - **&lt;metadata&gt;** — route name, description with distance / duration / fun rating, timestamp
	/// - **&lt;trk&gt;** — outbound track with one `&lt;trkpt&gt;` per polyline point
	/// - **&lt;trk&gt;** *(optional)* — return leg track, only present for `DifferentRoute` loop responses
	///
	/// ## Sample Request — Point-to-point or SameRoute loop
	/// ```json
	/// {
	///   "routeName": "Ardennes Twisty Run",
	///   "polyline": [[51.2093, 3.2247], [51.1500, 3.8000], [50.1415, 5.8792]],
	///   "distanceMeters": 185000,
	///   "estimatedDurationMinutes": 165,
	///   "funRating": 4,
	///   "funScore": 1.72
	/// }
	/// ```
	///
	/// ## Sample Request — DifferentRoute loop (outbound + return)
	/// ```json
	/// {
	///   "routeName": "Brussels Loop — Full Day",
	///   "polyline": [[50.8503, 4.3517], [50.5000, 5.1000], [50.8503, 4.3517]],
	///   "distanceMeters": 130000,
	///   "estimatedDurationMinutes": 240,
	///   "funRating": 5,
	///   "funScore": 2.61,
	///   "returnLegPolyline": [[50.8503, 4.3517], [50.6000, 4.8000], [50.8503, 4.3517]],
	///   "returnLegDistanceMeters": 125000,
	///   "returnLegDurationMinutes": 230,
	///   "returnLegFunRating": 4
	/// }
	/// ```
	/// </remarks>
	/// <param name="command">GPX export parameters (polyline and route metadata).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <response code="200">
	/// GPX file returned as <c>application/gpx+xml</c> with a <c>Content-Disposition: attachment</c> header.
	/// </response>
	/// <response code="400">Validation failed — invalid polyline or missing required fields.</response>
	[HttpPost("export/gpx")]
	[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ExportGpx([FromBody] ExportGpxCommand command,	CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);

		if (result.IsFailure)
			return HandleFailure(result);

		var response = result.Value;

		// Return as a downloadable GPX file.
		// application/gpx+xml is the registered IANA MIME type for GPX 1.1.
		// Content-Disposition: attachment causes browsers and apps to trigger a save/import dialog.
		var bytes = System.Text.Encoding.UTF8.GetBytes(response.GpxContent);

		return File(
			fileContents: bytes,
			contentType: "application/gpx+xml; charset=utf-8",
			fileDownloadName: response.FileName);
	}

	#endregion
}