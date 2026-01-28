using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public interface IRouteCandidateGenerator
{
	Task<IReadOnlyList<RouteCandidate>> GenerateAsync(RoutePlan plan, CancellationToken cancellationToken = default);
}
