using System.Text.Json.Serialization;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;

public sealed class ValhallaSummary
{
	[JsonPropertyName("time")]
	public double Time { get; set; } // seconds

	[JsonPropertyName("length")]
	public double Length { get; set; } // km (because units=kilometers)

	[JsonPropertyName("min_lat")]
	public double MinLat { get; set; }

	[JsonPropertyName("min_lon")]
	public double MinLon { get; set; }

	[JsonPropertyName("max_lat")]
	public double MaxLat { get; set; }

	[JsonPropertyName("max_lon")]
	public double MaxLon { get; set; }

	// optional flags (present in your JSON)
	[JsonPropertyName("has_highway")]
	public bool HasHighway { get; set; }

	[JsonPropertyName("has_toll")]
	public bool HasToll { get; set; }

	[JsonPropertyName("has_ferry")]
	public bool HasFerry { get; set; }

	[JsonPropertyName("has_time_restrictions")]
	public bool HasTimeRestrictions { get; set; }
}