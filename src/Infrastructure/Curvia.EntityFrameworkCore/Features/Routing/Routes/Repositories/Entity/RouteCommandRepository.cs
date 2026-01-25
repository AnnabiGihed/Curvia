using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Domain.Features.Routing.Routes.Repositories.Enitity;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Repositories.Entity;

public class RouteCommandRepository : BaseAsyncCommandRepository<Route, RouteId>, IRouteCommandRepository
{
	#region Constructor
	public RouteCommandRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}
