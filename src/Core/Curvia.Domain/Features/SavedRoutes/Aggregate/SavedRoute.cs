using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Entities;

namespace Curvia.Domain.Features.SavedRoutes.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a route that a user has explicitly saved to their profile.
///
///              AGGREGATE DESIGN:
///              ─────────────────────────────────────────────────────────────────
///              SavedRoute is its own aggregate (not a child of AppUser) because:
///                - It can be queried independently (community feed, search).
///                - It carries its own invariants (visibility, review lifecycle).
///                - It references Route by ID only (cross-aggregate boundary rule).
///
///              CROSS-AGGREGATE REFERENCE:
///              RouteId (Guid) is stored as a plain column — NOT a navigation property
///              to the Route aggregate. This is intentional DDD cross-aggregate
///              referencing: SavedRoute does not own or control Route.
///              Route geometry is fetched separately when needed via IRouteQueryRepository.
///
///              REVIEW LIFECYCLE:
///              A user submits one review per SavedRoute. Submitting again replaces
///              the previous review entirely (last-write-wins, review history is not kept).
///              Deleting the review is allowed, returning the SavedRoute to unreviewed state.
/// </summary>
public sealed class SavedRoute : AggregateRoot<SavedRouteId>
{
	#region Properties
	/// <summary>
	/// Reference to the generated Route aggregate by ID.
	/// Cross-aggregate reference — never navigated as an EF include inside this aggregate.
	/// </summary>
	public Guid RouteId { get; private set; }

	/// <summary>
	/// Optional private notes about this route (packing list, weather conditions, etc.).
	/// Max 1,000 chars. Never shown publicly regardless of Visibility.
	/// </summary>
	public string? Notes { get; private set; }

	/// <summary>
	/// The owner's review of this route. Null if no review has been submitted yet.
	/// Owned entity — stored in the same table (see SavedRouteConfiguration).
	/// </summary>
	public RouteReview? Review { get; private set; }

	/// <summary>
	/// User-given name for this saved route (e.g., "Weekend Ardennes loop").
	/// Max 200 chars.
	/// </summary>
	public string Name { get; private set; } = default!;

	/// <summary>The user who saved this route. FK to AppUsers table.</summary>
	public UserId UserId { get; private set; } = default!;

	/// <summary>
	/// Controls whether the route appears in the community feed.
	/// Default: Private.
	/// </summary>
	public SavedRouteVisibility Visibility { get; private set; }
	#endregion

	#region Constructors
	/// <summary>For EF Core.</summary>
	private SavedRoute() { }

	private SavedRoute(SavedRouteId id, UserId userId, Guid routeId, string name, string? notes, SavedRouteVisibility visibility)
		: base(id)
	{
		Name = name;
		Notes = notes;
		UserId = userId;
		RouteId = routeId;
		Visibility = visibility;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Saves a generated route to a user's profile.
	/// </summary>
	/// <param name="userId">The authenticated user's app-level ID.</param>
	/// <param name="routeId">The ID of the generated Route aggregate to save.</param>
	/// <param name="name">Display name for this saved route. Max 200 chars.</param>
	/// <param name="notes">Optional private notes. Max 1,000 chars.</param>
	/// <param name="visibility">Initial visibility. Defaults to Private.</param>
	public static Result<SavedRoute> Create(UserId userId, Guid routeId, string name, string? notes = null, SavedRouteVisibility visibility = SavedRouteVisibility.Private)
	{
		if (userId is null)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.UserId.Null", "UserId is required."));

		if (routeId == Guid.Empty)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.RouteId.Empty", "RouteId must not be empty."));

		if (string.IsNullOrWhiteSpace(name))
			return Result.Failure<SavedRoute>(new Error("SavedRoute.Name.Empty", "Route name must not be empty."));

		if (name.Length > 200)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.Name.TooLong", "Route name must not exceed 200 characters."));

		if (notes is not null && notes.Length > 1000)
			return Result.Failure<SavedRoute>(new Error("SavedRoute.Notes.TooLong", "Notes must not exceed 1,000 characters."));

		if (!Enum.IsDefined(visibility))
			return Result.Failure<SavedRoute>(new Error("SavedRoute.Visibility.Invalid", $"Unknown visibility value '{(int)visibility}'."));

		var id = new SavedRouteId(Guid.NewGuid());
		return Result.Success(new SavedRoute(id, userId, routeId, name.Trim(), notes?.Trim(), visibility));
	}
	#endregion

	#region Domain behaviours
	/// <summary>
	/// Submits or replaces the owner's review for this route.
	/// Calling this a second time overwrites the previous review.
	/// </summary>
	/// <param name="rating">1–5 star rating.</param>
	/// <param name="comment">Optional free-text comment. Max 2,000 chars.</param>
	public Result SubmitReview(int rating, string? comment)
	{
		var reviewResult = RouteReview.Create(rating, comment);
		if (reviewResult.IsFailure)
			return Result.Failure(reviewResult.Error, reviewResult.ResultExceptionType);

		Review = reviewResult.Value;
		return Result.Success();
	}

	/// <summary>
	/// Removes the review from this saved route, returning it to unreviewed state.
	/// </summary>
	public void DeleteReview() => Review = null;

	/// <summary>
	/// Updates the name and notes of the saved route.
	/// </summary>
	public Result Rename(string name, string? notes)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Result.Failure(new Error("SavedRoute.Name.Empty", "Route name must not be empty."));

		if (name.Length > 200)
			return Result.Failure(new Error("SavedRoute.Name.TooLong", "Route name must not exceed 200 characters."));

		if (notes is not null && notes.Length > 1000)
			return Result.Failure(new Error("SavedRoute.Notes.TooLong", "Notes must not exceed 1,000 characters."));

		Name = name.Trim();
		Notes = notes?.Trim();
		return Result.Success();
	}

	/// <summary>
	/// Changes the visibility of this saved route.
	/// </summary>
	public Result SetVisibility(SavedRouteVisibility visibility)
	{
		if (!Enum.IsDefined(visibility))
			return Result.Failure(new Error("SavedRoute.Visibility.Invalid", $"Unknown visibility value '{(int)visibility}'."));

		Visibility = visibility;
		return Result.Success();
	}

	/// <summary>
	/// Soft-deletes the saved route (and its embedded review).
	/// </summary>
	public void Remove(string actorId) => Delete(DateTime.UtcNow, actorId);
	#endregion
}