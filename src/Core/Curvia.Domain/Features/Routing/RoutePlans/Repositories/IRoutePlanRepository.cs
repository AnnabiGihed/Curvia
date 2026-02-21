using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Domain.Features.Routing.RoutePlans.Repositories;

public interface IRoutePlanRepository : IAsyncCommandRepository<RoutePlan, RoutePlanId>
{
}
