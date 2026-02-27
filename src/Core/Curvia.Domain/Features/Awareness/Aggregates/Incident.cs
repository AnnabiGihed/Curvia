using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Awareness.Enums;

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
	/// <summary>
	/// WGS84 latitude of the incident location.
	/// </summary>
	public double Latitude { get; private set; }

	/// <summary>
	/// WGS84 longitude of the incident location.
	/// </summary>
	public double Longitude { get; private set; }

	/// <summary>
	/// Optional human-readable description of the incident.
	/// </summary>
	public string? Description { get; private set; }

	/// <summary>
	/// UTC timestamp from which this incident is active.
	/// </summary>
	public DateTime ValidFromUtc { get; private set; }

	/// <summary>
	/// UTC timestamp after which this incident is no longer active.
	/// </summary>
	public DateTime ValidUntilUtc { get; private set; }

	/// <summary>
	/// UTC timestamp of the last successful sync that touched this record.
	/// </summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>
	/// Severity level of the incident.
	/// </summary>
	public IncidentSeverity Severity { get; private set; }

	/// <summary>
	/// Categorisation of the incident type.
	/// </summary>
	public IncidentType IncidentType { get; private set; }

	/// <summary>
	/// Short name of the data source (e.g. "datex2").
	/// </summary>
	public string Source { get; private set; } = default!;
	/// <summary>
	/// External identifier from the DATEX II source dataset.
	/// </summary>
	public string ExternalId { get; private set; } = default!;

	/// <summary>
	/// ISO 3166-1 alpha-2 country code (e.g. "BE", "FR").
	/// </summary>
	public string CountryCode { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private Incident() { }

	/// <summary>
	/// Initialises a fully-populated <see cref="Incident"/> instance.
	/// </summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="latitude">WGS84 latitude.</param>
	/// <param name="longitude">WGS84 longitude.</param>
	/// <param name="incidentType">Incident classification.</param>
	/// <param name="severity">Incident severity.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="validFromUtc">Start of the active window.</param>
	/// <param name="validUntilUtc">End of the active window.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private Incident(IncidentId id, string externalId, string source, string countryCode, double latitude, double longitude, IncidentType incidentType, IncidentSeverity severity, string? description, DateTime validFromUtc, DateTime validUntilUtc, DateTime lastSyncedUtc) : base(id)
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
	/// <summary>
	/// Creates a new <see cref="Incident"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is null or empty.
	/// </summary>
	/// <param name="externalId">Source dataset identifier. Must not be null or whitespace.</param>
	/// <param name="source">Short source name. Must not be null or whitespace.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code. Must not be null or whitespace.</param>
	/// <param name="latitude">WGS84 latitude of the incident location.</param>
	/// <param name="longitude">WGS84 longitude of the incident location.</param>
	/// <param name="incidentType">Incident classification.</param>
	/// <param name="severity">Incident severity level.</param>
	/// <param name="description">Optional human-readable description.</param>
	/// <param name="validFromUtc">UTC start of the active window.</param>
	/// <param name="validUntilUtc">UTC end of the active window.</param>
	/// <returns>A <see cref="Result{Incident}"/> containing the new aggregate, or a failure with an error.</returns>
	public static Result<Incident> Create(string externalId, string source, string countryCode, double latitude, double longitude, IncidentType incidentType, IncidentSeverity severity, string? description, DateTime validFromUtc, DateTime validUntilUtc)
	{
		if (string.IsNullOrWhiteSpace(externalId))
			return Result.Failure<Incident>(new Error("Incident.ExternalId.Required", "ExternalId is required."));

		if (string.IsNullOrWhiteSpace(source))
			return Result.Failure<Incident>(new Error("Incident.Source.Required", "Source is required."));

		if (string.IsNullOrWhiteSpace(countryCode))
			return Result.Failure<Incident>(new Error("Incident.CountryCode.Required", "CountryCode is required."));

		var now = DateTime.UtcNow;
		var incident = new Incident(IncidentId.New(), externalId, source, countryCode.ToUpperInvariant(), latitude, longitude, incidentType, severity, description, validFromUtc, validUntilUtc, now);
		incident.InitializeAudit(now, source);
		return Result.Success(incident);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates mutable fields when the Worker re-syncs an existing incident.
	/// ExternalId, Source, and CountryCode are immutable after creation.
	/// </summary>
	/// <param name="latitude">Updated WGS84 latitude.</param>
	/// <param name="longitude">Updated WGS84 longitude.</param>
	/// <param name="incidentType">Updated incident classification.</param>
	/// <param name="severity">Updated severity level.</param>
	/// <param name="description">Updated optional description.</param>
	/// <param name="validFromUtc">Updated start of the active window.</param>
	/// <param name="validUntilUtc">Updated end of the active window.</param>
	public void Sync(double latitude, double longitude, IncidentType incidentType, IncidentSeverity severity, string? description, DateTime validFromUtc, DateTime validUntilUtc)
	{
		Severity = severity;
		Latitude = latitude;
		Longitude = longitude;
		Description = description;
		IncidentType = incidentType;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source);
	}

	/// <summary>
	/// Returns true if the incident is currently active based on UTC now.
	/// </summary>
	public bool IsActive()
	{
		return DateTime.UtcNow >= ValidFromUtc && DateTime.UtcNow < ValidUntilUtc;
	}
	#endregion
}