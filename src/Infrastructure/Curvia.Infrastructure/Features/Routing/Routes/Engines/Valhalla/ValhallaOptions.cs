namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla;

public sealed class ValhallaOptions
{
	public string BaseUrl { get; init; } = "http://localhost:8002";
	public int TimeoutSeconds { get; init; } = 30;
}