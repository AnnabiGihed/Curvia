using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Aggregates;

namespace Curvia.Domain.Features.Motorcycles.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for <see cref="Motorcycle"/> persistence.
/// </summary>
public interface IMotorcycleRepository : IAsyncCommandRepository<Motorcycle, MotorcycleId>
{
	/// <summary>Returns the user's current default motorcycle, or null if none set.</summary>
	Task<Motorcycle?> FindDefaultAsync(UserId userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Clears IsDefault on every motorcycle owned by the user except the given one.
	/// Called by the handler before SetAsDefault() to maintain the one-default invariant.
	/// </summary>
	Task ClearDefaultAsync(UserId userId, MotorcycleId exceptId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all active motorcycles for the user, ordered by IsDefault descending,
	/// then creation date descending (default bike always first in list).
	/// </summary>
	Task<IReadOnlyList<Motorcycle>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns a single motorcycle by ID, null if not found / soft-deleted / wrong owner.
	/// </summary>
	Task<Motorcycle?> FindByIdAndUserAsync(MotorcycleId id, UserId userId, CancellationToken cancellationToken = default);
}





