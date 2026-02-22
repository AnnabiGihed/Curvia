using MediatR;
using Microsoft.AspNetCore.Mvc;
using Curvia.API.Middleware.Identity;
using Microsoft.AspNetCore.Authorization;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.SavedRoutes.Commands.Unsave;
using Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes;
using Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes.Responses;

namespace Curvia.API.Controllers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Saved route management and community feed endpoints.
/// </summary>
[Authorize]
[Route("api/saved-routes")]
public sealed class SavedRoutesController : ApiController
{
	#region Fields
	private readonly IAppUserContext _appUserContext;
	#endregion

	#region Constructor
	public SavedRoutesController(ISender sender, IAppUserContext appUserContext) : base(sender)
	{
		_appUserContext = appUserContext;
	}
	#endregion

	#region Authenticated user endpoints
	/// <summary>
	/// Removes a saved route from the authenticated user's profile.
	/// Only the owner of the saved route may unsave it.
	/// </summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UnsaveRoute([FromRoute] Guid id, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new UnsaveRouteCommand(userId, id), ct);
		return result.IsFailure ? HandleFailure(result) : NoContent();
	}
	#endregion

	#region Public endpoints
	/// <summary>
	/// Returns a paginated page of public saved routes from the community,
	/// ordered by average review rating descending then most recently saved.
	/// </summary>
	[HttpGet("community")]
	[ProducesResponseType(typeof(IReadOnlyList<PublicRouteDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetCommunityRoutes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		return HandleResult(await Sender.Send(new GetPublicRoutesQuery(page, pageSize), ct));
	}
	#endregion
}