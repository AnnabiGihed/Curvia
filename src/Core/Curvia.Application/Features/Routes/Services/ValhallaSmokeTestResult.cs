namespace Curvia.Application.Features.Routes.Services;

public sealed record ValhallaSmokeTestResult(double DistanceKm,	double TimeSeconds,	int LegsCount, bool HasShape, string? StatusMessage);