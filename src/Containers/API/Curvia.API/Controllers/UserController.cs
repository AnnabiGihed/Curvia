using MediatR;
using Microsoft.AspNetCore.Mvc;
using Curvia.API.Middleware.Identity;
using Microsoft.AspNetCore.Authorization;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.Users.Queries.GetProfile;
using Curvia.Application.Features.Users.Queries.GetProfile.Responses;

namespace Curvia.API.Controllers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : User profile endpoints.
///
///              All endpoints require authentication.
///              The AppUserId is resolved from the JWT by JitProvisioningMiddleware —
///              it is guaranteed to exist in the DB when any of these endpoints execute.
///
///              Endpoints:
///                GET /api/users/profile — returns the authenticated user's application profile
/// </summary>
[Authorize]
[Route("api/users")]
public sealed class UserController : ApiController
{
	#region Fields
	private readonly IAppUserContext _appUserContext;
	#endregion

	#region Constructor
	public UserController(ISender sender, IAppUserContext appUserContext) : base(sender)
	{
		_appUserContext = appUserContext;
	}
	#endregion

	#region GET /api/users/profile
	/// <summary>
	/// Returns the authenticated user's application profile.
	/// Profile fields (email, display name, locale) are kept in sync with Keycloak
	/// by the JIT provisioning middleware on every authenticated request.
	/// </summary>
	[HttpGet("profile")]
	[ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetProfile(CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		return HandleResult(await Sender.Send(new GetUserProfileQuery(userId), ct));
	}
	#endregion
}