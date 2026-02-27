using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Features.Awareness.ValueObjects;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a permanent road hazard relevant to motorcyclists.
///              Extends <see cref="AggregateRoot{TId}"/> — permanent hazards are static
///              reference data with no domain events or aggregate invariants beyond
///              the value-object constraints enforced at creation.
///              Lifecycle managed entirely by the Worker sync pipeline.
/// </summary>
public sealed class Hazard : AggregateRoot<HazardId>
{
	#region Properties
	/// <summary>WGS84 geographic position of the hazard.</summary>
	public Coordinate Position { get; private set; } = default!;

	/// <summary>Optional human-readable description of the hazard.</summary>
	public AwarenessDescription? Description { get; private set; }

	/// <summary>Categorisation of the hazard type.</summary>
	public HazardType HazardType { get; private set; }

	/// <summary>UTC timestamp of the last successful sync that touched this record.</summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>Short name of the data source that provided this hazard (e.g. "osm").</summary>
	public SourceName Source { get; private set; } = default!;

	/// <summary>External identifier from the source dataset (e.g. "osm:12345678").</summary>
	public ExternalId ExternalId { get; private set; } = default!;

	/// <summary>ISO 3166-1 alpha-2 country code (e.g. "BE", "FR").</summary>
	public AwarenessCountryCode CountryCode { get; private set; } = default!;
	#endregion


	/// <summary>WGS84 latitude convenience accessor. Equivalent to <see cref="Position"/>.Latitude.</summary>
	public double Latitude => Position.Latitude;

	/// <summary>WGS84 longitude convenience accessor. Equivalent to <see cref="Position"/>.Longitude.</summary>
	public double Longitude => Position.Longitude;

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private Hazard() { }

	/// <summary>Initialises a fully-populated <see cref="Hazard"/> instance.</summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="position">WGS84 position.</param>
	/// <param name="hazardType">Hazard classification.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private Hazard(HazardId id, ExternalId externalId, SourceName source, AwarenessCountryCode countryCode, Coordinate position, HazardType hazardType, AwarenessDescription? description, DateTime lastSyncedUtc) : base(id)
	{
		Source = source;
		Position = position;
		HazardType = hazardType;
		ExternalId = externalId;
		Description = description;
		CountryCode = countryCode;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="Hazard"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is invalid.
	/// </summary>
	/// <param name="externalId">Source dataset identifier. Must not be null or whitespace.</param>
	/// <param name="source">Short source name. Must not be null or whitespace.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code. Must not be null or whitespace.</param>
	/// <param name="latitude">WGS84 latitude of the hazard location.</param>
	/// <param name="longitude">WGS84 longitude of the hazard location.</param>
	/// <param name="hazardType">Hazard classification.</param>
	/// <param name="description">Optional human-readable description.</param>
	/// <returns>A <see cref="Result{Hazard}"/> containing the new aggregate, or a failure with an error.</returns>
	public static Result<Hazard> Create(string externalId, string source, string countryCode, double latitude, double longitude, HazardType hazardType, string? description)
	{
		var externalIdResult = ExternalId.Create(externalId);
		if (externalIdResult.IsFailure)
			return Result.Failure<Hazard>(externalIdResult.Error);

		var sourceResult = SourceName.Create(source);
		if (sourceResult.IsFailure)
			return Result.Failure<Hazard>(sourceResult.Error);

		var countryCodeResult = AwarenessCountryCode.Create(countryCode);
		if (countryCodeResult.IsFailure)
			return Result.Failure<Hazard>(countryCodeResult.Error);

		var positionResult = Coordinate.Create(latitude, longitude);
		if (positionResult.IsFailure)
			return Result.Failure<Hazard>(positionResult.Error);

		var descriptionResult = AwarenessDescription.CreateOptional(description);
		if (descriptionResult.IsFailure)
			return Result.Failure<Hazard>(descriptionResult.Error);

		var now = DateTime.UtcNow;
		var hazard = new Hazard(HazardId.New(), externalIdResult.Value, sourceResult.Value, countryCodeResult.Value, positionResult.Value, hazardType, descriptionResult.Value, now);
		hazard.InitializeAudit(now, source);
		return Result.Success(hazard);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates mutable fields when the Worker re-syncs an existing hazard.
	/// ExternalId, Source, and CountryCode are immutable after creation.
	/// </summary>
	/// <param name="latitude">Updated WGS84 latitude.</param>
	/// <param name="longitude">Updated WGS84 longitude.</param>
	/// <param name="hazardType">Updated hazard classification.</param>
	/// <param name="description">Updated optional description.</param>
	/// <returns>Success when updated; otherwise failure.</returns>
	public Result Sync(double latitude, double longitude, HazardType hazardType, string? description)
	{
		var positionResult = Coordinate.Create(latitude, longitude);
		if (positionResult.IsFailure)
			return Result.Failure(positionResult.Error);

		var descriptionResult = AwarenessDescription.CreateOptional(description);
		if (descriptionResult.IsFailure)
			return Result.Failure(descriptionResult.Error);

		Position = positionResult.Value;
		HazardType = hazardType;
		Description = descriptionResult.Value;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source.Value);
		return Result.Success();
	}
	#endregion
}