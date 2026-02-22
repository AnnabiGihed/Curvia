using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.Routes.Commands.Generate;
using Curvia.Application.Features.Routes.Commands.ExportGpx;
using Curvia.Application.Features.Routes.Commands.Generate.Responses;

namespace Curvia.API.Features.Routing.Routes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Exposes the motorcycle fun routing endpoints.
///              POST /api/routes            — generates the best fun route for a ride.
///              POST /api/routes/export/gpx — exports a computed route to GPX 1.1 format.
/// </summary>
[Authorize]
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
	/// <param name="command">Route generation parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <response code="200">Route generated successfully. Returns geometry, stats, fun rating and optional return leg.</response>
	/// <response code="400">Validation failed or no valid route found with the given constraints.</response>
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
	/// Exports a computed route to GPX 1.1 format for import into navigation devices and apps.
	/// </summary>
	/// <param name="command">GPX export parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <response code="200">GPX file as <c>application/gpx+xml</c> with Content-Disposition: attachment.</response>
	/// <response code="400">Validation failed — invalid polyline or missing required fields.</response>
	[HttpPost("export/gpx")]
	[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> ExportGpx([FromBody] ExportGpxCommand command, CancellationToken cancellationToken)
	{
		var result = await Sender.Send(command, cancellationToken);

		if (result.IsFailure)
			return HandleFailure(result);

		var response = result.Value;
		var bytes = System.Text.Encoding.UTF8.GetBytes(response.GpxContent);

		return File(fileContents: bytes, contentType: "application/gpx+xml; charset=utf-8", fileDownloadName: response.FileName);
	}
	#endregion
}