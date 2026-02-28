using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Routing.Routes.Aggregate;

namespace Curvia.Domain.Features.Routing.Routes.Repositories;

public interface IRouteRepository : IAsyncCommandRepository<Route, RouteId>
{
}
