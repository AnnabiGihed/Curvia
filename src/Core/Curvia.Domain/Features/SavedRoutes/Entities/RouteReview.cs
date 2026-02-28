using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;

namespace Curvia.Domain.Features.SavedRoutes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : A user's review of a saved route.
///              Entity within the <see cref="SavedRoute"/> aggregate boundary —
///              always accessed and mutated through SavedRoute domain methods.
/// </summary>
public sealed class RouteReview : Entity<RouteReviewId>
{
	#region Properties
	/// <summary>
	/// Optional free-text comment.
	/// Null when rating-only review submitted.
	/// </summary>
	public ReviewComment? Comment { get; private set; }

	/// <summary>
	/// UTC timestamp when the review was last submitted or updated.
	/// </summary>
	public DateTime ReviewedAtUtc { get; private set; }

	/// <summary>
	/// Star rating 1–5.
	/// </summary>
	public ReviewRating Rating { get; private set; } = default!;

	/// <summary>
	/// The user who wrote this review.
	/// May differ from the SavedRoute owner (community reviews).
	/// </summary>
	public UserId ReviewerUserId { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>
	/// For EF Core.
	/// </summary>
	private RouteReview() { }

	/// <summary>
	/// Initializes a new <see cref="RouteReview"/> entity.
	/// Sets <see cref="ReviewedAtUtc"/> to the current UTC time.
	/// </summary>
	/// <param name="id">Entity identifier.</param>
	/// <param name="reviewerUserId">Reviewer user identifier.</param>
	/// <param name="rating">Validated rating value object.</param>
	/// <param name="comment">Optional validated comment value object.</param>
	private RouteReview(RouteReviewId id, UserId reviewerUserId, ReviewRating rating, ReviewComment? comment)
		: base(id)
	{
		Rating = rating;
		Comment = comment;
		ReviewerUserId = reviewerUserId;
		ReviewedAtUtc = DateTime.UtcNow;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new review. Called only from <see cref="SavedRoute.SubmitReview"/>.
	/// </summary>
	/// <param name="reviewerUserId">Reviewer user identifier.</param>
	/// <param name="rating">Validated rating value object.</param>
	/// <param name="comment">Optional validated comment value object.</param>
	/// <returns>A successful result containing <see cref="RouteReview"/>, or a failure with domain error.</returns>
	internal static Result<RouteReview> Create(UserId reviewerUserId, ReviewRating rating, ReviewComment? comment)
	{
		if (reviewerUserId is null)
			return Result.Failure<RouteReview>(new Error("RouteReview.ReviewerUserId.Null", "ReviewerUserId is required."));

		return Result.Success(new RouteReview(new RouteReviewId(Guid.NewGuid()), reviewerUserId, rating, comment));
	}
	#endregion

	#region Domain behaviours
	/// <summary>
	/// Replaces rating and comment on an existing review.
	/// Called only from <see cref="SavedRoute.SubmitReview"/> when a review by this user already exists.
	/// </summary>
	/// <param name="rating">New validated rating value object.</param>
	/// <param name="comment">New optional validated comment value object.</param>
	internal void Update(ReviewRating rating, ReviewComment? comment)
	{
		Rating = rating;
		Comment = comment;
		ReviewedAtUtc = DateTime.UtcNow;
	}

	/// <summary>
	/// Soft-deletes the review.
	/// </summary>
	/// <param name="actorId">Identifier of the actor performing the removal.</param>
	internal void Remove(string actorId)
	{
		SoftDelete(DateTime.UtcNow, actorId);
	}
	#endregion
}