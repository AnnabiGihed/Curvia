using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : A user's review of a saved route.
///              Entity within the <see cref="SavedRoute"/> aggregate boundary —
///              always accessed and mutated through SavedRoute domain methods.
///
///              MULTIPLE REVIEWS PER SAVED ROUTE:
///              When a route is public, community riders can leave reviews.
///              Each review belongs to one reviewer (ReviewerUserId).
///              The aggregate enforces: one review per (SavedRouteId, ReviewerUserId).
///              Submitting a second review replaces the first (last-write-wins).
///
///              VALUE OBJECTS:
///                Rating  → ReviewRating  (1–5, domain-validated)
///                Comment → ReviewComment (optional, max 2,000 chars)
///
///              RATING SCALE:
///              1★ = Disappointing   2★ = Below average   3★ = Good
///              4★ = Great           5★ = Outstanding
/// </summary>
public sealed class RouteReview : Entity<RouteReviewId>
{
	#region Properties

	/// <summary>The user who wrote this review. May differ from the SavedRoute owner (community reviews).</summary>
	public UserId ReviewerUserId { get; private set; } = default!;

	/// <summary>Star rating 1–5.</summary>
	public ReviewRating Rating { get; private set; } = default!;

	/// <summary>Optional free-text comment. Null when rating-only review submitted.</summary>
	public ReviewComment? Comment { get; private set; }

	/// <summary>UTC timestamp when the review was last submitted or updated.</summary>
	public DateTime ReviewedAtUtc { get; private set; }

	#endregion

	#region Constructors

	/// <summary>For EF Core.</summary>
	private RouteReview() { }

	private RouteReview(RouteReviewId id, UserId reviewerUserId, ReviewRating rating, ReviewComment? comment)
		: base(id)
	{
		ReviewerUserId = reviewerUserId;
		Rating = rating;
		Comment = comment;
		ReviewedAtUtc = DateTime.UtcNow;
	}

	#endregion

	#region Factory

	/// <summary>
	/// Creates a new review. Called only from <see cref="SavedRoute.SubmitReview"/>.
	/// </summary>
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
	internal void Update(ReviewRating rating, ReviewComment? comment)
	{
		Rating = rating;
		Comment = comment;
		ReviewedAtUtc = DateTime.UtcNow;
	}

	/// <summary>Soft-deletes the review.</summary>
	internal void Remove(string actorId) => SoftDelete(DateTime.UtcNow, actorId);

	#endregion
}