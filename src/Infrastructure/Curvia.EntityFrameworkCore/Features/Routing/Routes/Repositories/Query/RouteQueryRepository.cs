using Curvia.Domain.Features.Routing.Routes.Projections;
using Curvia.Domain.Features.Routing.Routes.Repositories.Query;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Repositories.Query;

public class RouteQueryRepository : BaseAsyncQueryRepository<RouteProjection, RouteProjectionId>, IRouteQueryRepository
{
	#region Constructor
	public RouteQueryRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}
