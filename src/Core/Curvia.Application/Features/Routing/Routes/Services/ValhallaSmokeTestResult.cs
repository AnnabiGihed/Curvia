namespace Curvia.Application.Features.Routing.Routes.Services;

public sealed record ValhallaSmokeTestResult(double DistanceKm,	double TimeSeconds,	int LegsCount, bool HasShape, string? StatusMessage);