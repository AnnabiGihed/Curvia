using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Repositories;

public class RouteRepository : BaseAsyncCommandRepository<Route, RouteId>, IRouteRepository
{
	#region Constructor
	public RouteRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}