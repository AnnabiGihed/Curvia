using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Domain.Features.Routing.RoutePlans.Repositories.Entity;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Repositories.Entity;

internal class RoutePlanCommandRepository : BaseAsyncCommandRepository<RoutePlan, RoutePlanId>, IRoutePlanCommandRepository
{
	#region Constructor
	public RoutePlanCommandRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}
