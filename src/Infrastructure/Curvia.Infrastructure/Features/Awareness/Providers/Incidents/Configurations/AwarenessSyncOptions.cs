namespace Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Configuration options for DATEX II and equivalent incident feeds.
///              Loaded from appsettings.json "AwarenessSync:IncidentFeeds" section.
///              Adding a new country = adding one entry to the config file, no code change.
/// </summary>
public sealed class AwarenessSyncOptions
{
	public const string SectionName = "AwarenessSync";

	/// <summary>
	/// List of free incident feed configurations, one per country (or feed).
	/// </summary>
	public List<IncidentFeedConfig> IncidentFeeds { get; set; } = new();
}
