using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Features.Awareness.ValueObjects;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a live traffic incident relevant to motorcyclists
///              (e.g. accident, debris on road, sudden congestion).
///              Has a validity window (ValidFromUtc → ValidUntilUtc) used by the
///              scheduled cleanup job to prune expired records.
///              Synced every 3 minutes per country by Hangfire.
///              Lifecycle managed entirely by the Worker sync pipeline.
/// </summary>
public sealed class Incident : AggregateRoot<IncidentId>
{
	#region Properties
	/// <summary>WGS84 geographic position of the incident.</summary>
	public Coordinate Position { get; private set; } = default!;

	/// <summary>Incident classification.</summary>
	public IncidentType IncidentType { get; private set; }

	/// <summary>Severity level of the incident.</summary>
	public IncidentSeverity Severity { get; private set; }

	/// <summary>Optional human-readable description of the incident.</summary>
	public AwarenessDescription? Description { get; private set; }

	/// <summary>UTC start of the active window.</summary>
	public DateTime ValidFromUtc { get; private set; }

	/// <summary>UTC end of the active window.</summary>
	public DateTime ValidUntilUtc { get; private set; }

	/// <summary>UTC timestamp of the last successful sync that touched this record.</summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>Short name of the data source that provided this incident (e.g. "tomtom", "here").</summary>
	public SourceName Source { get; private set; } = default!;

	/// <summary>External identifier from the source dataset.</summary>
	public ExternalId ExternalId { get; private set; } = default!;

	/// <summary>ISO 3166-1 alpha-2 country code (e.g. "BE", "FR").</summary>
	public AwarenessCountryCode CountryCode { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private Incident() { }

	/// <summary>Initialises a fully-populated <see cref="Incident"/> instance.</summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="position">WGS84 position.</param>
	/// <param name="incidentType">Incident classification.</param>
	/// <param name="severity">Severity level.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="validFromUtc">Start of the active window.</param>
	/// <param name="validUntilUtc">End of the active window.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private Incident(IncidentId id, ExternalId externalId, SourceName source, AwarenessCountryCode countryCode, Coordinate position, IncidentType incidentType, IncidentSeverity severity, AwarenessDescription? description, DateTime validFromUtc, DateTime validUntilUtc, DateTime lastSyncedUtc) : base(id)
	{
		Source = source;
		Position = position;
		ExternalId = externalId;
		Severity = severity;
		IncidentType = incidentType;
		Description = description;
		CountryCode = countryCode;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="Incident"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is invalid.
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
		var externalIdResult = ExternalId.Create(externalId);
		if (externalIdResult.IsFailure)
			return Result.Failure<Incident>(externalIdResult.Error);

		var sourceResult = SourceName.Create(source);
		if (sourceResult.IsFailure)
			return Result.Failure<Incident>(sourceResult.Error);

		var countryCodeResult = AwarenessCountryCode.Create(countryCode);
		if (countryCodeResult.IsFailure)
			return Result.Failure<Incident>(countryCodeResult.Error);

		var positionResult = Coordinate.Create(latitude, longitude);
		if (positionResult.IsFailure)
			return Result.Failure<Incident>(positionResult.Error);

		var (descriptionVo, descriptionError) = AwarenessDescription.CreateOptional(description);
		if (descriptionError is not null)
			return Result.Failure<Incident>(descriptionError);

		var now = DateTime.UtcNow;
		var incident = new Incident(IncidentId.New(), externalIdResult.Value, sourceResult.Value, countryCodeResult.Value, positionResult.Value, incidentType, severity, descriptionVo, validFromUtc, validUntilUtc, now);
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
	/// <returns>Success when updated; otherwise failure.</returns>
	public Result Sync(double latitude, double longitude, IncidentType incidentType, IncidentSeverity severity, string? description, DateTime validFromUtc, DateTime validUntilUtc)
	{
		var positionResult = Coordinate.Create(latitude, longitude);
		if (positionResult.IsFailure)
			return Result.Failure(positionResult.Error);

		var (descriptionVo, descriptionError) = AwarenessDescription.CreateOptional(description);
		if (descriptionError is not null)
			return Result.Failure<Hazard>(descriptionError);

		Position = positionResult.Value;
		IncidentType = incidentType;
		Severity = severity;
		Description = descriptionVo;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source.Value);
		return Result.Success();
	}
	#endregion
}