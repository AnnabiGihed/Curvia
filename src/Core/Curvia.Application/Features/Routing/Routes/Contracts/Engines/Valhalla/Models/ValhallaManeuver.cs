using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

public sealed class ValhallaManeuver
{
	[JsonPropertyName("instruction")]
	public string? Instruction { get; set; }

	[JsonPropertyName("time")]
	public double Time { get; set; }

	[JsonPropertyName("length")]
	public double Length { get; set; }

	[JsonPropertyName("begin_shape_index")]
	public int BeginShapeIndex { get; set; }

	[JsonPropertyName("end_shape_index")]
	public int EndShapeIndex { get; set; }

	[JsonPropertyName("street_names")]
	public List<string>? StreetNames { get; set; }
}