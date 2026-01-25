using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Routing.Routes.Aggregate;

namespace Curvia.Domain.Features.Routing.Routes.Repositories.Enitity;

public interface IRouteCommandRepository : IAsyncCommandRepository<Route, RouteId>
{
}
