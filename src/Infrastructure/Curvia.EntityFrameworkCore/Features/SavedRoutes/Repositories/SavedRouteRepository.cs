using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="ISavedRouteRepository"/>.
///              Global query filter on SavedRoute (IsDeleted = false) is applied
///              automatically — all queries here return only active records.
///              RouteReview is included automatically as an owned type (same table).
/// </summary>
internal sealed class SavedRouteRepository : BaseAsyncCommandRepository<SavedRoute, SavedRouteId>, ISavedRouteRepository
{
	public SavedRouteRepository(CurviaDbContext dbContext) 
		: base(dbContext)
	{
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Where(sr => sr.UserId == userId)
			.OrderByDescending(sr => sr.Audit.CreatedOnUtc)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<SavedRoute?> FindByIdAndUserAsync(SavedRouteId id, UserId userId,CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.FirstOrDefaultAsync(sr => sr.Id == id && sr.UserId == userId, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<bool> AlreadySavedAsync(UserId userId, Guid routeId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.AnyAsync(sr => sr.UserId == userId && sr.RouteId == routeId, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetPublicRoutesAsync(int skip, int take, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Where(sr => sr.Visibility == SavedRouteVisibility.Public)
			.OrderByDescending(sr => sr.Review != null ? sr.Review.Rating : 0)
			.ThenByDescending(sr => sr.Audit.CreatedOnUtc)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);
	}
}
