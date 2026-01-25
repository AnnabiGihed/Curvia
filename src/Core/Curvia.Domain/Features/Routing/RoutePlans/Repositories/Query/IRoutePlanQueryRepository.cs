using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Routing.RoutePlans.Projections;

namespace Curvia.Domain.Features.Routing.RoutePlans.Repositories.Query;

public interface IRoutePlanQueryRepository : IAsyncQueryRepository<RoutePlanProjection, RoutePlanProjectionId>
{
}
