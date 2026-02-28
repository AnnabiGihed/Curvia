using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SubmitReviewCommand"/>.
///
///              Pipeline:
///                1. Build ReviewRating and ReviewComment value objects (fast-fail).
///                2. Resolve the route via a two-path lookup:
///                     Owner path      — FindByIdAndUserAsync: reviewer owns the route (any visibility).
///                     Community path  — FindByIdAsync + Visibility == Public check:
///                                       reviewer is a community rider on someone else's public route.
///                3. Delegate to aggregate.SubmitReview() — upserts the review.
///                4. Persist and return the submitted review from the collection.
///
///              WHY VOs ARE BUILT FIRST:
///              Building VOs before the DB query means we fail fast on invalid input
///              without paying the cost of a database round-trip for bad data.
///              FluentValidation mirrors these constraints, but the VO is the truth.
///
///              WHY TWO PATHS AND NOT ONE:
///              A single FindByIdAsync + manual ownership/visibility check would require
///              loading every route just to enforce access — including private routes
///              belonging to other users. The owner path short-circuits cleanly in the
///              common case. The community path only runs when the owner path returns null,
///              keeping private routes invisible to non-owners (null == not found == not accessible,
///              no information leakage).
/// </summary>
internal sealed class SubmitReviewCommandHandler : ICommandHandler<SubmitReviewCommand, SubmitReviewResponse>
{
	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SubmitReviewCommandHandler"/>.
	/// </summary>
	/// <param name="savedRouteRepository">Repository used to load and persist saved routes.</param>
	public SubmitReviewCommandHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Submits or replaces a reviewer's rating and comment on a saved route.
	/// The reviewer may be the route owner or a community rider reviewing any Public route.
	/// Idempotent: a second submission by the same reviewer replaces the first.
	/// </summary>
	/// <param name="command">Command containing the reviewer id, saved route id, rating and optional comment.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// A <see cref="SubmitReviewResponse"/> describing the submitted review,
	/// or a failure result when the route is not found, not accessible, or domain validation fails.
	/// </returns>
	public async Task<Result<SubmitReviewResponse>> Handle(SubmitReviewCommand command, CancellationToken cancellationToken)
	{
		// 1. Build value objects — fail before hitting the DB if input is invalid.
		var ratingResult = ReviewRating.Create(command.Rating);
		if (ratingResult.IsFailure)
			return Result.Failure<SubmitReviewResponse>(ratingResult.Error, ratingResult.ResultExceptionType);

		ReviewComment? commentVo = null;
		if (command.Comment is not null)
		{
			var commentResult = ReviewComment.Create(command.Comment);
			if (commentResult.IsFailure)
				return Result.Failure<SubmitReviewResponse>(commentResult.Error, commentResult.ResultExceptionType);
			commentVo = commentResult.Value;
		}

		var reviewerUserId = new UserId(command.UserId);
		var savedRouteId = new SavedRouteId(command.SavedRouteId);

		// 2. Resolve the route — owner path first, community path as fallback.
		var savedRoute = await ResolveReviewableRouteAsync(savedRouteId, reviewerUserId, cancellationToken);
		if (savedRoute is null)
			return Result.Failure<SubmitReviewResponse>(new Error("SavedRoute.NotFound", "The saved route was not found or is not available for review."));

		// 3. Delegate to aggregate — upserts review for this reviewer.
		var reviewResult = savedRoute.SubmitReview(reviewerUserId, ratingResult.Value, commentVo);
		if (reviewResult.IsFailure)
			return Result.Failure<SubmitReviewResponse>(reviewResult.Error, reviewResult.ResultExceptionType);

		// 4. Persist.
		await _savedRouteRepository.UpdateAsync(savedRoute, cancellationToken);

		// 5. Return the review that was just submitted.
		var submittedReview = savedRoute.Reviews
			.First(r => r.ReviewerUserId == reviewerUserId && !r.IsDeleted);

		return Result.Success(new SubmitReviewResponse(SavedRouteId: savedRoute.Id.Value, ReviewId: submittedReview.Id.Value, ReviewerUserId: submittedReview.ReviewerUserId.Value, Rating: submittedReview.Rating.Value, Comment: submittedReview.Comment?.Value, ReviewedAtUtc: submittedReview.ReviewedAtUtc));
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Resolves a saved route the reviewer is allowed to review using a two-path lookup.
	/// Owner path: <see cref="ISavedRouteRepository.FindByIdAndUserAsync"/> — succeeds when the
	/// reviewer owns the route regardless of its visibility.
	/// Community path: <see cref="ISavedRouteRepository.FindByIdAsync"/> restricted to
	/// <see cref="SavedRouteVisibility.Public"/> — succeeds when a community rider targets a public route.
	/// Returns null for non-existent routes, soft-deleted routes, and private routes belonging
	/// to other users so that none of these cases are distinguishable to the caller.
	/// </summary>
	/// <param name="savedRouteId">Identifier of the saved route to review.</param>
	/// <param name="reviewerUserId">Identifier of the authenticated reviewer.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The reviewable <see cref="SavedRoute"/>, or null when not accessible.</returns>
	private async Task<SavedRoute?> ResolveReviewableRouteAsync(SavedRouteId savedRouteId, UserId reviewerUserId, CancellationToken cancellationToken)
	{
		var ownedRoute = await _savedRouteRepository.FindByIdAndUserAsync(savedRouteId, reviewerUserId, cancellationToken);
		if (ownedRoute is not null)
			return ownedRoute;

		var route = await _savedRouteRepository.FindByIdAsync(savedRouteId, cancellationToken);
		if (route is not null && route.Visibility == SavedRouteVisibility.Public)
			return route;

		return null;
	}
	#endregion
}