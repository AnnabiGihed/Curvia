using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Routing.Routes.Projections;

namespace Curvia.Domain.Features.Routing.Routes.Repositories.Query;

public interface IRouteQueryRepository : IAsyncQueryRepository<RouteProjection, RouteProjectionId>
{
}
