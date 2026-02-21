using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;
using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

public sealed class ValhallaRouteResponse
{
	[JsonPropertyName("trip")]
	public ValhallaTrip Trip { get; set; } = default!;
}