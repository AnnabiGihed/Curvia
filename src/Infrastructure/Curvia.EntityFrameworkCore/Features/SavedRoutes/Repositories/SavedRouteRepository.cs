using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

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
	public async Task<SavedRoute?> FindByIdAsync(SavedRouteId id, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<bool> AlreadySavedAsync(UserId userId, RouteId routeId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.AnyAsync(sr => sr.UserId == userId && sr.RouteId == routeId, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.Where(sr => sr.UserId == userId)
			.OrderByDescending(sr => sr.Audit.CreatedOnUtc)
			.ToListAsync(cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<SavedRoute?> FindByIdAndUserAsync(SavedRouteId id, UserId userId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.FirstOrDefaultAsync(sr => sr.Id == id && sr.UserId == userId, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SavedRoute>> GetPublicRoutesAsync(int skip, int take, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<SavedRoute>()
			.Include(sr => sr.Reviews)
			.Where(sr => sr.Visibility == SavedRouteVisibility.Public)
			.OrderByDescending(sr => sr.Reviews.Any(r => !r.IsDeleted) ? sr.Reviews.Where(r => !r.IsDeleted).Average(r => r.Rating.Value) : 0)
			.ThenByDescending(sr => sr.Audit.CreatedOnUtc)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);
	}
	#endregion
}