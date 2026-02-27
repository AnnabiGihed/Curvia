using Curvia.Domain.Features.Awareness.Enums;
using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a live traffic incident sourced from DATEX II feeds.
///
///              Sources vary by country (all free with or without registration):
///                - Belgium/Flanders: Verkeerscentrum DATEX II
///                - Belgium/Wallonia: SPW DATEX II
///                - Netherlands:      NDW DATEX II
///                - France:           Bison Futé DATEX II
///                - Germany:          BASt open data
///                - Austria:          ASFINAG DATEX II
///                - More countries added incrementally via configuration
///
///              ValidUntilUtc drives visibility. The Worker polls every 2–3 minutes.
///              Expired incidents are excluded by repository queries automatically.
///
///              Coverage is concentrated on major roads. Rural road coverage
///              depends on user reporting (v2).
/// </summary>
public sealed class Incident : AggregateRoot<IncidentId>
{
	#region Properties
	public string ExternalId { get; private set; } = default!;
	public string Source { get; private set; } = default!;
	public string CountryCode { get; private set; } = default!;
	public double Latitude { get; private set; }
	public double Longitude { get; private set; }
	public IncidentType IncidentType { get; private set; }
	public IncidentSeverity Severity { get; private set; }
	public string? Description { get; private set; }
	public DateTime ValidFromUtc { get; private set; }
	public DateTime ValidUntilUtc { get; private set; }
	public DateTime LastSyncedUtc { get; private set; }
	#endregion

	#region Constructor
	private Incident() { }

	private Incident(
		IncidentId id,
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		IncidentType incidentType,
		IncidentSeverity severity,
		string? description,
		DateTime validFromUtc,
		DateTime validUntilUtc,
		DateTime lastSyncedUtc)
		: base(id)
	{
		ExternalId = externalId;
		Source = source;
		CountryCode = countryCode;
		Latitude = latitude;
		Longitude = longitude;
		IncidentType = incidentType;
		Severity = severity;
		Description = description;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	public static Incident Create(
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		IncidentType incidentType,
		IncidentSeverity severity,
		string? description,
		DateTime validFromUtc,
		DateTime validUntilUtc)
	{
		var now = DateTime.UtcNow;
		var incident = new Incident(
			IncidentId.New(),
			externalId,
			source,
			countryCode.ToUpperInvariant(),
			latitude,
			longitude,
			incidentType,
			severity,
			description,
			validFromUtc,
			validUntilUtc,
			now);

		incident.InitializeAudit(now, source);
		return incident;
	}
	#endregion

	#region Domain behaviour
	public void Sync(
		double latitude,
		double longitude,
		IncidentType incidentType,
		IncidentSeverity severity,
		string? description,
		DateTime validFromUtc,
		DateTime validUntilUtc)
	{
		Latitude = latitude;
		Longitude = longitude;
		IncidentType = incidentType;
		Severity = severity;
		Description = description;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source);
	}

	/// <summary>Returns true if the incident is currently active based on UTC now.</summary>
	public bool IsActive() => DateTime.UtcNow >= ValidFromUtc && DateTime.UtcNow < ValidUntilUtc;
	#endregion
}