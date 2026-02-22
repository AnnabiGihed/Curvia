using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Domain.Repositories;

namespace Curvia.Domain.Features.SavedRoutes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for <see cref="SavedRoute"/> persistence.
///
///              NOTE: AlreadySavedAsync and GetPublicRoutesAsync take RouteId (not Guid)
///              now that SavedRoute.RouteId is a strongly-typed RouteId value object.
/// </summary>
public interface ISavedRouteRepository : IAsyncCommandRepository<SavedRoute, SavedRouteId>
{
	/// <summary>
	/// Returns true if the user has already saved the given route.
	/// Prevents duplicate (UserId, RouteId) entries.
	/// </summary>
	Task<bool> AlreadySavedAsync(
		UserId userId, RouteId routeId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all active saved routes for the user, most recent first.
	/// Includes the full reviews collection for each route.
	/// </summary>
	Task<IReadOnlyList<SavedRoute>> GetByUserIdAsync(
		UserId userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns public saved routes across all users for the community feed,
	/// ordered by average review rating descending, then creation date descending.
	/// </summary>
	Task<IReadOnlyList<SavedRoute>> GetPublicRoutesAsync(
		int skip, int take, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns a single saved route with its reviews collection.
	/// Returns null if not found, soft-deleted, or not owned by the given user.
	/// </summary>
	Task<SavedRoute?> FindByIdAndUserAsync(
		SavedRouteId id, UserId userId, CancellationToken cancellationToken = default);
}