using MediatR;
using Microsoft.AspNetCore.Mvc;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

namespace Curvia.API.Controllers;

[ApiController]
[Route("[controller]")]
public class RouteController : ApiController
{
	#region Properties
	protected readonly ILogger<RouteController> _logger;
	#endregion

	#region Constructor
	public RouteController(ISender sender, ILogger<RouteController> logger)
		: base(sender)
	{
		_logger = logger;
	}
	#endregion

	#region Commands
	/// <summary>
	/// Generates a fun-optimized motorcycle route.
	/// </summary>
	/// <remarks>
	/// Supply either (EndLatitude + EndLongitude) for a point-to-point route,
	/// or (LoopTargetDistanceMeters) for a loop starting and ending at the same location.
	///
	/// Sample point-to-point request (Bruges → Ghent, Belgium):
	///
	///     POST /api/routes
	///     {
	///       "startLatitude": 51.2093,
	///       "startLongitude": 3.2247,
	///       "endLatitude": 51.0543,
	///       "endLongitude": 3.7174,
	///       "avoidHighways": false,
	///       "avoidTolls": false,
	///       "maxDetourRatio": 2.0,
	///       "funFactor": 8.0,
	///       "curvesWeight": 0.7,
	///       "elevationWeight": 0.2,
	///       "sceneryWeight": 0.1
	///     }
	/// </remarks>
	/// <param name="command">Route generation parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The best-scoring route candidate with geometry and stats.</returns>
	[HttpPost]
	[ProducesResponseType(typeof(GenerateRouteResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> GenerateRoute([FromBody] GenerateRouteCommand command,	CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);
		return HandleResult(result);
	}
	#endregion

	#region Queries
	#endregion
}
