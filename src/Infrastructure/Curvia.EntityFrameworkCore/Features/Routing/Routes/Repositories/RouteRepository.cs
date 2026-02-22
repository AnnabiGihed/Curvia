using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IRouteRepository"/>.
///              Provides command-capable repository behavior for <see cref="Route"/> aggregates
///              via <see cref="BaseAsyncCommandRepository{TEntity,TId}"/>.
/// </summary>
public class RouteRepository : BaseAsyncCommandRepository<Route, RouteId>, IRouteRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="RouteRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public RouteRepository(CurviaDbContext dbContext)
		: base(dbContext)
	{
	}
	#endregion
}