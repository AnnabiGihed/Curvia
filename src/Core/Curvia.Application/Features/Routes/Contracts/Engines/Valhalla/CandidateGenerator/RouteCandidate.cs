using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

public sealed record RouteCandidate(ValhallaRouteResponse Raw, string VariantName);