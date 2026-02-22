using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;

/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="ISavedRouteRepository"/>.
///
///              IMPORTANT CHANGES FROM PREVIOUS VERSION:
///              ─────────────────────────────────────────────────────────────────
///              1. AlreadySavedAsync takes RouteId (strongly-typed) instead of raw Guid.
///
///              2. All queries now call .Include(x => x.Reviews) because Reviews is an
///                 owned entity collection in a separate table (OwnsMany → RouteReviews).
///                 Without Include, Reviews would always be an empty collection.
///
///              3. GetPublicRoutesAsync ordering changed: single Review is now a
///                 collection. We order by the average rating across all non-deleted
///                 reviews, falling back to 0 when the route has no reviews yet.
///                 Average is computed server-side via EF's AVG translation.
///
///              Global query filter (IsDeleted = false) applies automatically to SavedRoute.
///              Soft-deleted reviews inside the collection are filtered in-memory after
///              load — EF does not support HasQueryFilter on owned entity collections.
/// </summary>
internal sealed class SavedRouteRepository
	: BaseAsyncCommandRepository<SavedRoute, SavedRouteId>, ISavedRouteRepository
{
	public SavedRouteRepository(CurviaDbContext dbContext) : base(dbContext) { }

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetByUserIdAsync(
		UserId userId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.Where(sr => sr.UserId == userId)
			.OrderByDescending(sr => sr.Audit.CreatedOnUtc)
			.ToListAsync(cancellationToken);

	/// <inheritdoc/>
	public async Task<SavedRoute?> FindByIdAndUserAsync(
		SavedRouteId id, UserId userId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.FirstOrDefaultAsync(sr => sr.Id == id && sr.UserId == userId, cancellationToken);

	/// <inheritdoc/>
	public async Task<bool> AlreadySavedAsync(
		UserId userId, RouteId routeId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<SavedRoute>()
			.AnyAsync(sr => sr.UserId == userId && sr.RouteId == routeId, cancellationToken);

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetPublicRoutesAsync(
		int skip, int take, CancellationToken cancellationToken = default)
		=> await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.Where(sr => sr.Visibility == SavedRouteVisibility.Public)
			// Order by average review rating desc; routes with no reviews sort to the bottom.
			.OrderByDescending(sr => sr.Reviews.Any(r => !r.IsDeleted)
				? sr.Reviews.Where(r => !r.IsDeleted).Average(r => r.Rating.Value)
				: 0)
			.ThenByDescending(sr => sr.Audit.CreatedOnUtc)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);
}