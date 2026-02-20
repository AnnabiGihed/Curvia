using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public sealed class ValhallaRouteResponse
{
	[JsonPropertyName("trip")]
	public ValhallaTrip Trip { get; set; } = default!;
}