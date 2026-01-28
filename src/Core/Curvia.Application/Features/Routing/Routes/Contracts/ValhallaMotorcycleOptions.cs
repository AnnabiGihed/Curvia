namespace Curvia.Application.Features.Routing.Routes.Contracts;

public sealed record ValhallaMotorcycleOptions(bool UseHighways, bool UseTolls,	double? HighwayFactor, double? TopSpeedKph);