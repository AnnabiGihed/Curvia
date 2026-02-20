using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public sealed class ValhallaLocationResult
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("lat")]
	public double Lat { get; set; }

	[JsonPropertyName("lon")]
	public double Lon { get; set; }
}