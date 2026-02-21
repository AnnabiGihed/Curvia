using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Builder;

public interface IRouteBuilder
{
	Result<Route> Build(RoutePlan plan, ValhallaRouteResponse response, string graphVersionId);
}