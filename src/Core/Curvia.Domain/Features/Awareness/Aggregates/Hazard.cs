using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a permanent road hazard relevant to motorcyclists.
///              Extends <see cref="AggregateRoot{TId}"/> — permanent hazards are static
///              reference data with no domain events or aggregate invariants.
///              Lifecycle managed entirely by the Worker sync pipeline.
/// </summary>
public sealed class Hazard : AggregateRoot<HazardId>
{
	#region Properties
	/// <summary>
	/// WGS84 latitude of the hazard location.
	/// </summary>
	public double Latitude { get; private set; }

	/// <summary>
	/// WGS84 longitude of the hazard location.
	/// </summary>
	public double Longitude { get; private set; }

	/// <summary>
	/// Optional human-readable description of the hazard.
	/// </summary>
	public string? Description { get; private set; }

	/// <summary>
	/// Categorisation of the hazard type.
	/// </summary>
	public HazardType HazardType { get; private set; }

	/// <summary>
	/// UTC timestamp of the last successful sync that touched this record.
	/// </summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>
	/// Short name of the data source that provided this hazard (e.g. "osm").
	/// </summary>
	public string Source { get; private set; } = default!;

	/// <summary>
	/// External identifier from the source dataset (e.g. "osm:12345678").
	/// </summary>
	public string ExternalId { get; private set; } = default!;

	/// <summary>
	/// ISO 3166-1 alpha-2 country code (e.g. "BE", "FR").
	/// </summary>
	public string CountryCode { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>
	/// For EF Core materialization only.
	/// </summary>
	private Hazard() { }

	/// <summary>
	/// Initialises a fully-populated <see cref="Hazard"/> instance.
	/// </summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="latitude">WGS84 latitude.</param>
	/// <param name="longitude">WGS84 longitude.</param>
	/// <param name="hazardType">Hazard classification.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private Hazard(HazardId id, string externalId, string source, string countryCode, double latitude, double longitude, HazardType hazardType, string? description, DateTime lastSyncedUtc) : base(id)
	{
		ExternalId = externalId;
		Source = source;
		CountryCode = countryCode;
		Latitude = latitude;
		Longitude = longitude;
		HazardType = hazardType;
		Description = description;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="Hazard"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is null or empty.
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
		if (string.IsNullOrWhiteSpace(externalId))
			return Result.Failure<Hazard>(new Error("Hazard.ExternalId.Required", "ExternalId is required."));

		if (string.IsNullOrWhiteSpace(source))
			return Result.Failure<Hazard>(new Error("Hazard.Source.Required", "Source is required."));

		if (string.IsNullOrWhiteSpace(countryCode))
			return Result.Failure<Hazard>(new Error("Hazard.CountryCode.Required", "CountryCode is required."));

		var now = DateTime.UtcNow;
		var hazard = new Hazard(HazardId.New(), externalId, source, countryCode.ToUpperInvariant(), latitude, longitude, hazardType, description, now);
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
	public void Sync(double latitude, double longitude, HazardType hazardType, string? description)
	{
		Latitude = latitude;
		Longitude = longitude;
		HazardType = hazardType;
		Description = description;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source);
	}
	#endregion
}