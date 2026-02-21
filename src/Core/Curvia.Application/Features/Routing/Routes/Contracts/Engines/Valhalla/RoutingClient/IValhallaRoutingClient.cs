namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

public interface IValhallaRoutingClient
{
	Task<ValhallaRouteResponse> RouteAsync(ValhallaRouteRequest request, CancellationToken cancellationToken = default);
}