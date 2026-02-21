namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

public sealed record RouteScoredCandidate(RouteCandidate Candidate, double Score);