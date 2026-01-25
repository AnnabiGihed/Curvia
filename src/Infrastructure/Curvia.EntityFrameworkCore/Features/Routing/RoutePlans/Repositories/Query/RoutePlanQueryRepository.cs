using Curvia.Domain.Features.Routing.RoutePlans.Projections;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Domain.Features.Routing.RoutePlans.Repositories.Query;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Repositories.Query;

public class RoutePlanQueryRepository : BaseAsyncQueryRepository<RoutePlanProjection, RoutePlanProjectionId>, IRoutePlanQueryRepository
{
	#region Constructor
	public RoutePlanQueryRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}
