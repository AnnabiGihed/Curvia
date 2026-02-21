using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SubmitReviewCommand"/>.
///
///              Pipeline:
///                1. Load the SavedRoute (with ownership guard — user must own it).
///                2. Delegate to aggregate.SubmitReview() to enforce domain rules.
///                3. Persist and return.
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
	public async Task<Result<SubmitReviewResponse>> Handle(SubmitReviewCommand command, CancellationToken cancellationToken)
	{
		// 1. Load with ownership guard
		var userId = new UserId(command.UserId);
		var savedRouteId = new SavedRouteId(command.SavedRouteId);

		var savedRoute = await _savedRouteRepository.FindByIdAndUserAsync(savedRouteId, userId, cancellationToken);

		if (savedRoute is null)
			return Result.Failure<SubmitReviewResponse>(new Error("SavedRoute.NotFound", "The saved route was not found or does not belong to your profile."));

		// 2. Delegate to aggregate
		var reviewResult = savedRoute.SubmitReview(command.Rating, command.Comment);
		if (reviewResult.IsFailure)
			return Result.Failure<SubmitReviewResponse>(reviewResult.Error, reviewResult.ResultExceptionType);

		// 3. Persist
		await _savedRouteRepository.UpdateAsync(savedRoute, cancellationToken);

		return Result.Success(new SubmitReviewResponse(SavedRouteId: savedRoute.Id.Value, Rating: savedRoute.Review!.Rating, Comment: savedRoute.Review.Comment, ReviewedAtUtc: savedRoute.Review.ReviewedAtUtc));
	}
	#endregion
}