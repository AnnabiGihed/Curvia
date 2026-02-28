using Curvia.Application.Features.Awareness.Queries.GetAwarenessForArea;
using Curvia.Application.Features.Awareness.Queries.GetAwarenessForRoute;
using Curvia.Application.Features.Awareness.Responses;
using Curvia.Domain.Features.Awareness.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pivot.Framework.Containers.API.Abstractions;

namespace Curvia.API.Controllers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Awareness query endpoints powering the map overlay and ride-mode alerts.
///
///              Endpoints:
///                GET /api/awareness/area
///                  Returns awareness items within a WGS84 bounding box.
///                  Public — no authentication required.
///                  Use case: map exploration mode, camera overlay toggle.
///
///                GET /api/awareness/route/{routeId}
///                  Returns awareness items along a route corridor.
///                  Requires authentication.
///                  Use case: pre-ride alert fetch, navigation mode refresh.
///
///              Both endpoints accept a ?types= filter so mobile clients can
///              request only the categories they display (e.g. cameras + incidents only).
///              If types is omitted, all four categories are returned.
///
///              The response includes DataFreshnessUtc — the oldest LastSyncedUtc
///              across returned items. Clients should display a warning when
///              freshness is more than the expected sync interval old.
/// </summary>
[Route("api/awareness")]
public sealed class AwarenessController : ApiController
{
	public AwarenessController(ISender sender) : base(sender) { }

	#region GET /api/awareness/area
	/// <summary>
	/// Returns awareness items within the specified WGS84 bounding box.
	/// Maximum area: 4° × 4° (≈ 400 km × 400 km).
	/// </summary>
	/// <param name="minLat">Southern boundary latitude.</param>
	/// <param name="minLon">Western boundary longitude.</param>
	/// <param name="maxLat">Northern boundary latitude.</param>
	/// <param name="maxLon">Eastern boundary longitude.</param>
	/// <param name="types">Comma-separated list of item types to include. Defaults to all.</param>
	[HttpGet("area")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(AwarenessResultDto), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> GetForArea(
		[FromQuery] double minLat,
		[FromQuery] double minLon,
		[FromQuery] double maxLat,
		[FromQuery] double maxLon,
		[FromQuery] AwarenessItemType[] types = default!,
		CancellationToken ct = default)
	{
		var resolvedTypes = types is { Length: > 0 }
			? types
			: Enum.GetValues<AwarenessItemType>();

		var query = new GetAwarenessForAreaQuery(minLat, minLon, maxLat, maxLon, resolvedTypes);
		return HandleResult(await Sender.Send(query, ct));
	}
	#endregion

	#region GET /api/awareness/route/{routeId}
	/// <summary>
	/// Returns awareness items within a corridor around the specified route.
	/// </summary>
	/// <param name="routeId">Identifier of the route to query the corridor for.</param>
	/// <param name="bufferMeters">Width of the corridor on each side in metres. Default: 500. Max: 2000.</param>
	/// <param name="types">Comma-separated list of item types. Defaults to all.</param>
	[HttpGet("route/{routeId:guid}")]
	[Authorize]
	[ProducesResponseType(typeof(AwarenessResultDto), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetForRoute(
		[FromRoute] Guid routeId,
		[FromQuery] double bufferMeters = 500,
		[FromQuery] AwarenessItemType[] types = default!,
		CancellationToken ct = default)
	{
		var resolvedTypes = types is { Length: > 0 }
			? types
			: Enum.GetValues<AwarenessItemType>();

		var query = new GetAwarenessForRouteQuery(routeId, bufferMeters, resolvedTypes);
		return HandleResult(await Sender.Send(query, ct));
	}
	#endregion
}