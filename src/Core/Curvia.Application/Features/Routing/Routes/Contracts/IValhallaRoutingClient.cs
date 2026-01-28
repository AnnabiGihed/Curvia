namespace Curvia.Application.Features.Routing.Routes.Contracts;

public interface IValhallaRoutingClient
{
	Task<ValhallaRouteResponse> RouteAsync(ValhallaRouteRequest request, CancellationToken cancellationToken = default);
}