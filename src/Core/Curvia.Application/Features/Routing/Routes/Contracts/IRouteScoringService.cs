using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public interface IRouteScoringService
{
	double Score(ValhallaRouteResponse candidate, RoutePlan plan, string variantName);
}