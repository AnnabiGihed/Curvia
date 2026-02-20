using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Templates.Core.Containers.API.Abstractions;

namespace Curvia.API.Features.Routing.Routes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Exposes the motorcycle fun routing endpoints.
///              POST /api/routes — generates the best fun route for a ride.
/// </summary>
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

	#region Endpoints

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
	public async Task<IActionResult> GenerateRoute(
		[FromBody] GenerateRouteCommand command,
		CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);
		return HandleResult(result);
	}

	#endregion
}