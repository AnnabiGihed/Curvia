using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

public sealed record RouteCandidate(ValhallaRouteResponse Raw, string VariantName);