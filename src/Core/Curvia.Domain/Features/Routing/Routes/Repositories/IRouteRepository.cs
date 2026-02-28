using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Routing.Routes.Aggregate;

namespace Curvia.Domain.Features.Routing.Routes.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for <see cref="Route"/> aggregate persistence.
///              Exposes command-capable repository operations (add, update, find by ID, delete)
///              via the <see cref="IAsyncCommandRepository{TEntity,TId}"/> contract.
///              Query-side reads are handled through lightweight read models, not this interface.
/// </summary>
public interface IRouteRepository : IAsyncCommandRepository<Route, RouteId>
{
}