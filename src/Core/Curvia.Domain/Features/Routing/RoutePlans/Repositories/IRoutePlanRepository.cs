using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Domain.Features.Routing.RoutePlans.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for <see cref="RoutePlan"/> aggregate persistence.
///              Exposes command-capable repository operations (add, update, find by ID, delete)
///              via the <see cref="IAsyncCommandRepository{TEntity,TId}"/> contract.
///              RoutePlan aggregates are short-lived — created during route generation and retained
///              for reproducibility (graph version replay) and route-scoring audit trails.
/// </summary>
public interface IRoutePlanRepository : IAsyncCommandRepository<RoutePlan, RoutePlanId>
{
}