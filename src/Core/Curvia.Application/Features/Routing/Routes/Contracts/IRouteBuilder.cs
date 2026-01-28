using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public interface IRouteBuilder
{
	Result<Route> Build(RoutePlan plan, ValhallaRouteResponse response, string graphVersionId);
}