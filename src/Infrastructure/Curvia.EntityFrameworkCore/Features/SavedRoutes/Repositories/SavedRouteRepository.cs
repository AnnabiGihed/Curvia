using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="ISavedRouteRepository"/>.
/// </summary>
internal sealed class SavedRouteRepository : BaseAsyncCommandRepository<SavedRoute, SavedRouteId>, ISavedRouteRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SavedRouteRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public SavedRouteRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region ISavedRouteRepository
	/// <inheritdoc/>
	public async Task<bool> AlreadySavedAsync(UserId userId, RouteId routeId, CancellationToken ct = default)
	{
		return await DbContext.Set<SavedRoute>()
			.AnyAsync(sr => sr.UserId == userId && sr.RouteId == routeId, ct);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.Where(sr => sr.UserId == userId)
			.OrderByDescending(sr => sr.Audit.CreatedOnUtc)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<SavedRoute?> FindByIdAndUserAsync(SavedRouteId id, UserId userId, CancellationToken ct = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.FirstOrDefaultAsync(sr => sr.Id == id && sr.UserId == userId, ct);
	}

	/// <inheritdoc/>
	/// <remarks>
	/// TODO (L-2): For long-term scalability, add a denormalized <c>AverageRating FLOAT</c>
	/// column to the SavedRoutes table, updated by <c>SavedRoute.SubmitReview</c>.
	/// That will allow a pure-SQL ORDER BY without loading reviews in-memory.
	/// </remarks>
	public async Task<IReadOnlyList<SavedRoute>> GetPublicRoutesAsync(int skip, int take, CancellationToken ct = default)
	{
		// Step 1: Load public routes ordered by creation date descending (DB-side, fully translatable).
		// We over-fetch by a small factor to give the in-memory sort enough data to select the top N.
		// For the expected page sizes (≤ 50) this is negligible overhead.
		var routes = await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.Where(sr => sr.Visibility == SavedRouteVisibility.Public)
			.OrderByDescending(sr => sr.Audit.CreatedOnUtc)
			.Skip(skip)
			.Take(take)
			.ToListAsync(ct);

		// Step 2: Sort in-memory by average rating (descending), then by creation date (descending) as tiebreaker.
		// Rating.Value is a C# int property on the ReviewRating value object — safe to use in LINQ-to-Objects.
		return routes
			.OrderByDescending(sr =>
				sr.Reviews.Any(r => !r.IsDeleted)
					? sr.Reviews.Where(r => !r.IsDeleted).Average(r => r.Rating.Value)
					: 0.0)
			.ThenByDescending(sr => sr.Audit.CreatedOnUtc)
			.ToList();
	}
	#endregion
}