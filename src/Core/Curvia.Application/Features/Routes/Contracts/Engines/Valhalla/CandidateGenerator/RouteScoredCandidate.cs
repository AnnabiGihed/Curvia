namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

public sealed record RouteScoredCandidate(RouteCandidate Candidate, double Score);