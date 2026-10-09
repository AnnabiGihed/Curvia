namespace Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 03-2026
/// Purpose     : Configuration options for the TomTom Traffic Incidents provider.
///              A single API key covers all supported countries — no per-country
///              URL configuration is required.
///              Loaded from appsettings.json "TomTom" section.
/// </summary>
public sealed class TomTomSyncOptions
{
	public const string SectionName = "TomTom";

	/// <summary>TomTom API key obtained from developer.tomtom.com.</summary>
	public string ApiKey { get; set; } = default!;
}
