using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Entities;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;

namespace Curvia.Domain.Features.SavedRoutes.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a route that a user has explicitly saved to their profile.
/// </summary>
public sealed class SavedRoute : AggregateRoot<SavedRouteId>
{
	#region Fields
	private readonly List<RouteReview> _reviews = new();
	#endregion

	#region Properties
	/// <summary>
	/// Optional private rider notes.
	/// Max 1,000 chars.
	/// Never shown publicly.
	/// </summary>
	public SavedRouteDescription? Description { get; private set; }

	/// <summary>
	/// The user who saved this route.
	/// FK to AppUsers table.
	/// </summary>
	public UserId UserId { get; private set; } = default!;

	/// <summary>
	/// User-given name for this saved route.
	/// Max 200 chars.
	/// </summary>
	public SavedRouteTitle Title { get; private set; } = default!;

	/// <summary>
	/// Cross-aggregate reference to the generated Route.
	/// Strongly typed as RouteId (not raw Guid) for domain clarity.
	/// No EF navigation — Route is fetched independently when geometry is needed.
	/// </summary>
	public RouteId RouteId { get; private set; } = default!;

	/// <summary>
	/// Controls whether the route appears in the community feed.
	/// Default: Private.
	/// </summary>
	public SavedRouteVisibility Visibility { get; private set; }

	/// <summary>
	/// Reviews left on this route by community riders.
	/// Invariant: at most one review per ReviewerUserId (enforced by SubmitReview).
	/// </summary>
	public IReadOnlyCollection<RouteReview> Reviews => _reviews.AsReadOnly();
	#endregion

	#region Constructors
	/// <summary>
	/// For EF Core.
	/// </summary>
	private SavedRoute() { }

	/// <summary>
	/// Initializes a new <see cref="SavedRoute"/> aggregate.
	/// </summary>
	/// <param name="id">Aggregate identifier.</param>
	/// <param name="userId">Owning user identifier.</param>
	/// <param name="routeId">Referenced route identifier.</param>
	/// <param name="name">Validated route name.</param>
	/// <param name="notes">Optional validated notes.</param>
	/// <param name="visibility">Route visibility setting.</param>
	private SavedRoute(SavedRouteId id, UserId userId, RouteId routeId, SavedRouteTitle name, SavedRouteDescription? notes, SavedRouteVisibility visibility)
		: base(id)
	{
		Title = name;
		Description = notes;
		UserId = userId;
		RouteId = routeId;
		Visibility = visibility;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Saves a generated route to a user's profile.
	/// </summary>
	/// <param name="userId">Owning user identifier.</param>
	/// <param name="routeId">Referenced route identifier.</param>
	/// <param name="name">User-given name for the saved route.</param>
	/// <param name="notes">Optional private notes.</param>
	/// <param name="visibility">Initial visibility (defaults to Private).</param>
	/// <returns>A successful result containing the created <see cref="SavedRoute"/>, or a failure with domain error.</returns>
	public static Result<SavedRoute> Create(UserId userId, RouteId routeId, string name, string? notes = null, SavedRouteVisibility visibility = SavedRouteVisibility.Private)
	{
		if (userId is null)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.UserId.Null", "UserId is required."));

		if (routeId is null)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.RouteId.Null", "RouteId is required."));

		var nameResult = SavedRouteTitle.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure<SavedRoute>(nameResult.Error, nameResult.ResultExceptionType);

		SavedRouteDescription? notesVo = null;
		if (notes is not null)
		{
			var notesResult = SavedRouteDescription.Create(notes);
			if (notesResult.IsFailure)
				return Result.Failure<SavedRoute>(notesResult.Error, notesResult.ResultExceptionType);

			notesVo = notesResult.Value;
		}

		if (!Enum.IsDefined(visibility))
			return Result.Failure<SavedRoute>(new Error("SavedRoute.Visibility.Invalid", $"Unknown visibility value '{(int)visibility}'."));

		return Result.Success(new SavedRoute(new SavedRouteId(Guid.NewGuid()), userId, routeId, nameResult.Value, notesVo, visibility));
	}
	#endregion

	#region Domain behaviours
	/// <summary>
	/// Soft-deletes this saved route and all its reviews.
	/// </summary>
	/// <param name="actorId">Identifier of the actor performing the removal.</param>
	public void Remove(string actorId)
	{
		Delete(DateTime.UtcNow, actorId);
	}

	/// <summary>
	/// Updates the name and optional private notes.
	/// </summary>
	/// <param name="name">New user-given route name.</param>
	/// <param name="notes">New optional private notes.</param>
	/// <returns>Success when updated; otherwise failure with domain error.</returns>
	public Result Rename(string name, string? notes)
	{
		var nameResult = SavedRouteTitle.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure(nameResult.Error, nameResult.ResultExceptionType);

		SavedRouteDescription? notesVo = null;
		if (notes is not null)
		{
			var notesResult = SavedRouteDescription.Create(notes);
			if (notesResult.IsFailure)
				return Result.Failure(notesResult.Error, notesResult.ResultExceptionType);

			notesVo = notesResult.Value;
		}

		Title = nameResult.Value;
		Description = notesVo;

		return Result.Success();
	}

	/// <summary>
	/// Changes the visibility of this saved route.
	/// </summary>
	/// <param name="visibility">New visibility value.</param>
	/// <returns>Success when updated; otherwise failure with domain error.</returns>
	public Result SetVisibility(SavedRouteVisibility visibility)
	{
		if (!Enum.IsDefined(visibility))
			return Result.Failure(new Error("SavedRoute.Visibility.Invalid", $"Unknown visibility value '{(int)visibility}'."));

		Visibility = visibility;
		return Result.Success();
	}

	/// <summary>
	/// Removes a specific reviewer's review.
	/// Only that reviewer (or an admin) should call this.
	/// </summary>
	/// <param name="reviewerUserId">Reviewer user identifier.</param>
	/// <param name="actorId">Identifier of the actor performing the removal.</param>
	/// <returns>Success when removed; otherwise failure with domain error.</returns>
	public Result DeleteReview(UserId reviewerUserId, string actorId)
	{
		if (reviewerUserId is null)
			return Result.Failure(new Error("SavedRoute.ReviewerUserId.Null", "ReviewerUserId is required."));

		var review = _reviews.FirstOrDefault(r => r.ReviewerUserId == reviewerUserId);
		if (review is null)
			return Result.Failure(new Error("SavedRoute.Review.NotFound", "No review by this user was found."));

		review.Remove(actorId);

		return Result.Success();
	}

	/// <summary>
	/// Submits or replaces a reviewer's review on this saved route.
	/// If the reviewer already has a review, it is updated in-place.
	/// If not, a new review is added to the collection.
	/// </summary>
	/// <param name="reviewerUserId">The user submitting the review (may be the route owner or a community rider).</param>
	/// <param name="rating">1–5 star rating.</param>
	/// <param name="comment">Optional free-text comment. Max 2,000 chars.</param>
	/// <returns>Success when submitted; otherwise failure with domain error.</returns>
	public Result SubmitReview(UserId reviewerUserId, ReviewRating rating, ReviewComment? comment)
	{
		if (reviewerUserId is null)
			return Result.Failure(new Error("SavedRoute.ReviewerUserId.Null", "ReviewerUserId is required."));

		var existing = _reviews.FirstOrDefault(r => r.ReviewerUserId == reviewerUserId);
		if (existing is not null)
		{
			existing.Update(rating, comment);
			return Result.Success();
		}

		var reviewResult = RouteReview.Create(reviewerUserId, rating, comment);
		if (reviewResult.IsFailure)
			return Result.Failure(reviewResult.Error, reviewResult.ResultExceptionType);

		_reviews.Add(reviewResult.Value);

		return Result.Success();
	}
	#endregion
}