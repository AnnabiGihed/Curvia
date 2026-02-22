using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Entities;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a route that a user has explicitly saved to their profile.
///
///              AGGREGATE DESIGN:
///              ─────────────────────────────────────────────────────────────────
///              Own aggregate (not child of User) — it has independent invariants,
///              can appear in community feeds, and references Route by ID only (DDD rule).
///
///              MULTIPLE REVIEWS:
///              When Visibility = Public, community riders can leave reviews.
///              Invariant: one review per (SavedRouteId, ReviewerUserId).
///              Submitting a second review by the same user replaces the first.
///              The owner may also review their own route.
///
///              VALUE OBJECTS:
///                Name   → RouteName   (required, max 200 chars)
///                Notes  → RouteNotes? (optional private rider notes, max 1,000 chars)
///
///              ROUTEID CROSS-AGGREGATE REFERENCE:
///              RouteId is typed as the domain's RouteId strong type (not raw Guid).
///              This makes the reference explicit and type-safe at the domain level
///              while remaining a plain FK column in the DB (no EF navigation to Route).
/// </summary>
public sealed class SavedRoute : AggregateRoot<SavedRouteId>
{
	#region Fields
	private readonly List<RouteReview> _reviews = new();
	#endregion

	#region Properties

	/// <summary>The user who saved this route. FK to AppUsers table.</summary>
	public UserId UserId { get; private set; } = default!;

	/// <summary>
	/// Cross-aggregate reference to the generated Route.
	/// Strongly typed as RouteId (not raw Guid) for domain clarity.
	/// No EF navigation — Route is fetched independently when geometry is needed.
	/// </summary>
	public RouteId RouteId { get; private set; } = default!;

	/// <summary>User-given name for this saved route. Max 200 chars.</summary>
	public RouteName Name { get; private set; } = default!;

	/// <summary>Optional private rider notes. Max 1,000 chars. Never shown publicly.</summary>
	public RouteNotes? Notes { get; private set; }

	/// <summary>Controls whether the route appears in the community feed. Default: Private.</summary>
	public SavedRouteVisibility Visibility { get; private set; }

	/// <summary>
	/// Reviews left on this route by community riders.
	/// Invariant: at most one review per ReviewerUserId (enforced by SubmitReview).
	/// </summary>
	public IReadOnlyCollection<RouteReview> Reviews => _reviews.AsReadOnly();

	#endregion

	#region Constructors

	/// <summary>For EF Core.</summary>
	private SavedRoute() { }

	private SavedRoute(SavedRouteId id, UserId userId, RouteId routeId, RouteName name, RouteNotes? notes, SavedRouteVisibility visibility)
		: base(id)
	{
		UserId = userId;
		RouteId = routeId;
		Name = name;
		Notes = notes;
		Visibility = visibility;
	}

	#endregion

	#region Factory

	/// <summary>Saves a generated route to a user's profile.</summary>
	public static Result<SavedRoute> Create(
		UserId userId,
		RouteId routeId,
		string name,
		string? notes = null,
		SavedRouteVisibility visibility = SavedRouteVisibility.Private)
	{
		if (userId is null)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.UserId.Null", "UserId is required."));

		if (routeId is null)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.RouteId.Null", "RouteId is required."));

		var nameResult = RouteName.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure<SavedRoute>(nameResult.Error, nameResult.ResultExceptionType);

		RouteNotes? notesVo = null;
		if (notes is not null)
		{
			var notesResult = RouteNotes.Create(notes);
			if (notesResult.IsFailure)
				return Result.Failure<SavedRoute>(notesResult.Error, notesResult.ResultExceptionType);
			notesVo = notesResult.Value;
		}

		if (!Enum.IsDefined(visibility))
			return Result.Failure<SavedRoute>(new Error("SavedRoute.Visibility.Invalid", $"Unknown visibility value '{(int)visibility}'."));

		var id = new SavedRouteId(Guid.NewGuid());
		return Result.Success(new SavedRoute(id, userId, routeId, nameResult.Value, notesVo, visibility));
	}

	#endregion

	#region Domain behaviours

	/// <summary>
	/// Submits or replaces a reviewer's review on this saved route.
	/// If the reviewer already has a review, it is updated in-place.
	/// If not, a new review is added to the collection.
	/// </summary>
	/// <param name="reviewerUserId">The user submitting the review (may be the route owner or a community rider).</param>
	/// <param name="rating">1–5 star rating.</param>
	/// <param name="comment">Optional free-text comment. Max 2,000 chars.</param>
	public Result SubmitReview(UserId reviewerUserId, ReviewRating rating, ReviewComment? comment)
	{
		if (reviewerUserId is null)
			return Result.Failure(new Error("SavedRoute.ReviewerUserId.Null", "ReviewerUserId is required."));

		// Replace existing review by this user rather than accumulating duplicates.
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

	/// <summary>Removes a specific reviewer's review. Only that reviewer (or an admin) should call this.</summary>
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

	/// <summary>Updates the name and optional private notes.</summary>
	public Result Rename(string name, string? notes)
	{
		var nameResult = RouteName.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure(nameResult.Error, nameResult.ResultExceptionType);

		RouteNotes? notesVo = null;
		if (notes is not null)
		{
			var notesResult = RouteNotes.Create(notes);
			if (notesResult.IsFailure)
				return Result.Failure(notesResult.Error, notesResult.ResultExceptionType);
			notesVo = notesResult.Value;
		}

		Name = nameResult.Value;
		Notes = notesVo;
		return Result.Success();
	}

	/// <summary>Changes the visibility of this saved route.</summary>
	public Result SetVisibility(SavedRouteVisibility visibility)
	{
		if (!Enum.IsDefined(visibility))
			return Result.Failure(new Error("SavedRoute.Visibility.Invalid", $"Unknown visibility value '{(int)visibility}'."));

		Visibility = visibility;
		return Result.Success();
	}

	/// <summary>Soft-deletes this saved route and all its reviews.</summary>
	public void Remove(string actorId) => Delete(DateTime.UtcNow, actorId);

	#endregion
}