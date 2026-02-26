using MediatR;
using Microsoft.AspNetCore.Mvc;
using Curvia.API.DTOs.SavedRoutes;
using Curvia.API.Middleware.Identity;
using Microsoft.AspNetCore.Authorization;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.SavedRoutes.Commands.Save;
using Curvia.Application.Features.SavedRoutes.Commands.Unsave;
using Curvia.Application.Features.SavedRoutes.Commands.Update;
using Curvia.Application.Features.SavedRoutes.Commands.Review;
using Curvia.Application.Features.Users.Queries.GetSavedRoutes;
using Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;
using Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes;
using Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;
using Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;
using Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes.Responses;

namespace Curvia.API.Controllers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Saved route management and community feed endpoints.
///
///              Authenticated user endpoints:
///                POST   /api/saved-routes              — save a generated route to the user's profile
///                GET    /api/saved-routes              — list the user's saved routes (with reviews)
///                PUT    /api/saved-routes/{id}         — update name, notes and/or visibility
///                DELETE /api/saved-routes/{id}         — unsave a route (soft-delete)
///                POST   /api/saved-routes/{id}/reviews — submit or replace a review on a route
///
///              Public endpoints:
///                GET    /api/saved-routes/community    — paginated public route feed (by average rating)
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

	#region POST /api/saved-routes
	/// <summary>
	/// Saves a generated route to the authenticated user's profile.
	/// Returns 409 Conflict if the same route has already been saved.
	/// </summary>
	[HttpPost]
	[ProducesResponseType(typeof(SaveRouteResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
	public async Task<IActionResult> SaveRoute([FromBody] SaveRouteRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var command = new SaveRouteCommand(UserId: userId, RouteId: request.RouteId, Name: request.Name, Notes: request.Notes, Visibility: request.Visibility);

		var result = await Sender.Send(command, ct);

		if (result.IsFailure)
			return HandleFailure(result);

		return CreatedAtAction(nameof(GetMySavedRoutes), null, result.Value);
	}
	#endregion

	#region GET /api/saved-routes
	/// <summary>
	/// Returns all saved routes in the authenticated user's profile, including embedded reviews.
	/// Routes are ordered most recently saved first.
	/// </summary>
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<SavedRouteDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetMySavedRoutes(CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		return HandleResult(await Sender.Send(new GetUserSavedRoutesQuery(userId), ct));
	}
	#endregion

	#region PUT /api/saved-routes/{id}
	/// <summary>
	/// Updates the name, private notes and visibility of a saved route.
	/// Only the owner may update their saved route.
	/// </summary>
	[HttpPut("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> UpdateSavedRoute([FromRoute] Guid id, [FromBody] UpdateSavedRouteRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var command = new UpdateSavedRouteCommand(UserId: userId, SavedRouteId: id, Name: request.Name, Notes: request.Notes, Visibility: request.Visibility);

		var result = await Sender.Send(command, ct);
		return result.IsFailure ? HandleFailure(result) : NoContent();
	}
	#endregion

	#region DELETE /api/saved-routes/{id}
	/// <summary>
	/// Removes a saved route from the authenticated user's profile (soft-delete).
	/// Only the owner of the saved route may unsave it.
	/// </summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> UnsaveRoute([FromRoute] Guid id, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new UnsaveRouteCommand(userId, id), ct);
		return result.IsFailure ? HandleFailure(result) : NoContent();
	}
	#endregion

	#region POST /api/saved-routes/{id}/reviews
	/// <summary>
	/// Submits or replaces a review (1–5 stars + optional comment) on a saved route.
	/// The reviewer may be the route owner (any visibility) or a community rider (Public routes only).
	/// Idempotent: submitting again by the same user replaces the previous review.
	/// </summary>
	[HttpPost("{id:guid}/reviews")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(SubmitReviewResponse), StatusCodes.Status201Created)]
	public async Task<IActionResult> SubmitReview([FromRoute] Guid id, [FromBody] SubmitReviewRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var command = new SubmitReviewCommand(UserId: userId, SavedRouteId: id, Rating: request.Rating, Comment: request.Comment);

		var result = await Sender.Send(command, ct);

		if (result.IsFailure)
			return HandleFailure(result);

		return CreatedAtAction(nameof(GetMySavedRoutes), null, result.Value);
	}
	#endregion

	#region GET /api/saved-routes/community
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