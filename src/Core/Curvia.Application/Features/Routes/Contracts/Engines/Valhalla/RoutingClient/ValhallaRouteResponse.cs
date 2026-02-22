using System.Text.Json.Serialization;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Models;

namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

public sealed class ValhallaRouteResponse
{
	[JsonPropertyName("trip")]
	public ValhallaTrip Trip { get; set; } = default!;
}