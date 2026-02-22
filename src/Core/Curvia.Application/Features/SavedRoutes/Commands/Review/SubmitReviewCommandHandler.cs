using Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SubmitReviewCommand"/>.
///
///              Pipeline:
///                1. Build ReviewRating and ReviewComment value objects (fast-fail).
///                2. Load the SavedRoute with its reviews collection.
///                3. Ownership/visibility check: the user must own the route
///                   OR the route must be Public (community review).
///                4. Delegate to aggregate.SubmitReview() — upserts the review.
///                5. Persist and return the submitted review from the collection.
///
///              WHY VOs ARE BUILT FIRST:
///              Building VOs before the DB query means we fail fast on invalid input
///              without paying the cost of a database round-trip for bad data.
///              FluentValidation mirrors these constraints, but the VO is the truth.
/// </summary>
internal sealed class SubmitReviewCommandHandler : ICommandHandler<SubmitReviewCommand, SubmitReviewResponse>
{
	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	public SubmitReviewCommandHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result<SubmitReviewResponse>> Handle(
		SubmitReviewCommand command, CancellationToken cancellationToken)
	{
		// 1. Build value objects — fail before hitting the DB if input is invalid
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

		// 2. Load the saved route with its reviews collection.
		//    FindByIdAndUserAsync enforces ownership (route must belong to this user).
		//    For community reviews on public routes, a separate query method would be used —
		//    this handler currently covers the owner-review case.
		var savedRoute = await _savedRouteRepository.FindByIdAndUserAsync(
			savedRouteId, reviewerUserId, cancellationToken);

		if (savedRoute is null)
			return Result.Failure<SubmitReviewResponse>(new Error(
				"SavedRoute.NotFound",
				"The saved route was not found or does not belong to your profile."));

		// 3. Delegate to aggregate — upserts review for this reviewer
		var reviewResult = savedRoute.SubmitReview(reviewerUserId, ratingResult.Value, commentVo);
		if (reviewResult.IsFailure)
			return Result.Failure<SubmitReviewResponse>(reviewResult.Error, reviewResult.ResultExceptionType);

		// 4. Persist
		await _savedRouteRepository.UpdateAsync(savedRoute, cancellationToken);

		// 5. Return the review that was just submitted (find it in the collection by reviewer)
		var submittedReview = savedRoute.Reviews
			.First(r => r.ReviewerUserId == reviewerUserId && !r.IsDeleted);

		return Result.Success(new SubmitReviewResponse(
			SavedRouteId: savedRoute.Id.Value,
			ReviewId: submittedReview.Id.Value,
			ReviewerUserId: submittedReview.ReviewerUserId.Value,
			Rating: submittedReview.Rating.Value,   // unwrap ReviewRating VO
			Comment: submittedReview.Comment?.Value, // unwrap ReviewComment? VO
			ReviewedAtUtc: submittedReview.ReviewedAtUtc));
	}
	#endregion
}