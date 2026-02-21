using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.SavedRoutes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : The owner's review of a saved route.
///              Owned entity within the <see cref="SavedRoute"/> aggregate boundary —
///              it has no independent lifecycle and is always accessed through SavedRoute.
///
///              ONE REVIEW PER SAVED ROUTE:
///              The 1:1 relationship is enforced by the aggregate: SavedRoute exposes
///              a single nullable Review property. To update, the user replaces it
///              entirely via SavedRoute.SubmitReview().
///
///              RATING SCALE:
///              1★ = Disappointing    (route was worse than expected)
///              2★ = Below average    (some interest, not worth repeating)
///              3★ = Good             (enjoyable, would ride again)
///              4★ = Great            (excellent roads, recommended)
///              5★ = Outstanding      (one of the best rides, must-do)
/// </summary>
public sealed class RouteReview : Entity<RouteReviewId>
{
	#region Properties
	/// <summary>
	/// Star rating from 1 to 5. Stored as an int column, validated by domain.
	/// </summary>
	public int Rating { get; private set; }

	/// <summary>
	/// Optional free-text review. Max 2000 chars.
	/// Null when the user submitted a rating-only review.
	/// </summary>
	public string? Comment { get; private set; }

	/// <summary>UTC timestamp when the review was last written or updated.</summary>
	public DateTime ReviewedAtUtc { get; private set; }
	#endregion

	#region Constructors
	/// <summary>For EF Core.</summary>
	private RouteReview() { }

	private RouteReview(RouteReviewId id, int rating, string? comment, DateTime reviewedAtUtc)
	{
		Id = id;
		Rating = rating;
		Comment = comment;
		ReviewedAtUtc = reviewedAtUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a <see cref="RouteReview"/>.
	/// Called only from <see cref="SavedRoute.SubmitReview"/>.
	/// </summary>
	/// <param name="rating">1–5 star rating.</param>
	/// <param name="comment">Optional text review. Max 2,000 characters.</param>
	internal static Result<RouteReview> Create(int rating, string? comment)
	{
		if (rating is < 1 or > 5)
			return Result.Failure<RouteReview>(new Error("Review.Rating.OutOfRange", $"Rating must be between 1 and 5. Got {rating}."));

		if (comment is not null && comment.Length > 2000)
			return Result.Failure<RouteReview>(new Error("Review.Comment.TooLong", "Review comment must not exceed 2,000 characters."));

		return Result.Success(new RouteReview(new RouteReviewId(Guid.NewGuid()), rating, string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(), DateTime.UtcNow));
	}
	#endregion
}