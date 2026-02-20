using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts;

public sealed class ValhallaLeg
{
	[JsonPropertyName("shape")]
	public string Shape { get; set; } = default!;

	[JsonPropertyName("summary")]
	public ValhallaSummary Summary { get; set; } = default!;

	[JsonPropertyName("maneuvers")]
	public List<ValhallaManeuver> Maneuvers { get; set; } = new();
}