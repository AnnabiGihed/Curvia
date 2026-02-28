using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IRouteRepository"/>.
///              Provides command-capable repository behaviour for <see cref="Route"/> aggregates
///              via <see cref="BaseAsyncCommandRepository{TEntity,TId}"/>.
///              Sealed — not designed for inheritance.
///              Internal — implementation detail of the persistence layer; consumers depend on
///              <see cref="IRouteRepository"/> only.
/// </summary>
internal sealed class RouteRepository : BaseAsyncCommandRepository<Route, RouteId>, IRouteRepository
{
	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="RouteRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context injected by the DI container.</param>
	public RouteRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion
}