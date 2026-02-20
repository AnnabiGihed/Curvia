using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public sealed class ValhallaTrip
{
	[JsonPropertyName("status")]
	public int Status { get; set; }

	[JsonPropertyName("status_message")]
	public string? StatusMessage { get; set; }

	[JsonPropertyName("units")]
	public string? Units { get; set; }

	[JsonPropertyName("language")]
	public string? Language { get; set; }

	[JsonPropertyName("summary")]
	public ValhallaSummary Summary { get; set; } = default!;

	[JsonPropertyName("legs")]
	public List<ValhallaLeg> Legs { get; set; } = new();

	[JsonPropertyName("locations")]
	public List<ValhallaLocationResult> Locations { get; set; } = new();
}