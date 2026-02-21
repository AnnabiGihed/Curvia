using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;

namespace Curvia.Domain.Features.SavedRoutes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Application port for <see cref="SavedRoute"/> persistence.
///              Extends base command operations with read queries required by the handlers.
/// </summary>
public interface ISavedRouteRepository : IAsyncCommandRepository<SavedRoute, SavedRouteId>
{
	/// <summary>
	/// Returns true if the user has already saved the given route.
	/// Prevents duplicate SavedRoute entries for the same (userId, routeId) pair.
	/// </summary>
	Task<bool> AlreadySavedAsync(UserId userId, Guid routeId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all active saved routes for the given user, ordered by creation date descending.
	/// Includes embedded reviews where present.
	/// </summary>
	Task<IReadOnlyList<SavedRoute>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all public saved routes across all users, ordered by review rating descending.
	/// Used for the community feed. Excludes soft-deleted entries.
	/// </summary>
	Task<IReadOnlyList<SavedRoute>> GetPublicRoutesAsync(int skip, int take, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns a single saved route by ID.
	/// Returns null if not found, soft-deleted, or not owned by the given user.
	/// The userId guard prevents users from accessing each other's private routes.
	/// </summary>
	Task<SavedRoute?> FindByIdAndUserAsync(SavedRouteId id, UserId userId, CancellationToken cancellationToken = default);
}