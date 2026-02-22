using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IRoutePlanRepository"/>.
///              Provides command-capable repository behavior for <see cref="RoutePlan"/> aggregates
///              via <see cref="BaseAsyncCommandRepository{TEntity,TId}"/>.
/// </summary>
internal class RoutePlanRepository : BaseAsyncCommandRepository<RoutePlan, RoutePlanId>, IRoutePlanRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="RoutePlanRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public RoutePlanRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}