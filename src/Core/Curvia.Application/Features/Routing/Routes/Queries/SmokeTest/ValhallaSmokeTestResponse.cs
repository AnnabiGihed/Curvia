namespace Curvia.Application.Features.Routing.Routes.Queries.SmokeTest;

public sealed record ValhallaSmokeTestResponse(double DistanceKm, double TimeSeconds, int LegsCount, bool HasShape,	string? StatusMessage);