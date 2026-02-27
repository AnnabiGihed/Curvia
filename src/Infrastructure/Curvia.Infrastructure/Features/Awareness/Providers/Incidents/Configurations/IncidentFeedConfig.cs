namespace Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;


/// <summary>Configuration for a single country's incident feed.</summary>
public sealed class IncidentFeedConfig
{
	/// <summary>ISO 3166-1 alpha-2 country code.</summary>
	public string CountryCode { get; set; } = default!;

	/// <summary>Feed URL (DATEX II JSON/XML endpoint or equivalent).</summary>
	public string Url { get; set; } = default!;

	/// <summary>Feed format identifier. Supported: "DatexII_JSON", "DatexII_XML", "NdwJson".</summary>
	public string Format { get; set; } = "DatexII_JSON";

	/// <summary>Optional Bearer token or API key for feeds requiring free registration.</summary>
	public string? ApiKey { get; set; }
}