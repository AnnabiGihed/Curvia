using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Repositories;

internal class RoutePlanRepository : BaseAsyncCommandRepository<RoutePlan, RoutePlanId>, IRoutePlanRepository
{
	#region Constructor
	public RoutePlanRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}
