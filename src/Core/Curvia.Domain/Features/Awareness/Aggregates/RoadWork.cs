using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a temporary road work or planned obstruction.
///
///              Sources vary by country:
///                - Belgium: GIPOD API (government-mandated registration)
///                - Other countries: OSM construction tags, or country-specific feeds
///
///              ValidUntilUtc drives visibility — expired road works are automatically
///              excluded by repository queries without needing a separate cleanup job.
///
///              The Worker syncs these every 4–6 hours per country.
/// </summary>
public sealed class RoadWork : AggregateRoot<RoadWorkId>
{
	#region Properties
	/// <summary>
	/// WGS84 latitude of the road work location.
	/// </summary>
	public double Latitude { get; private set; }

	/// <summary>
	/// WGS84 longitude of the road work location.
	/// </summary>
	public double Longitude { get; private set; }

	/// <summary>
	/// Optional extended description of the road work.
	/// </summary>
	public string? Description { get; private set; }

	/// <summary>
	/// UTC timestamp from which this road work is active.
	/// </summary>
	public DateTime ValidFromUtc { get; private set; }

	/// <summary>
	/// UTC timestamp after which this road work is no longer active and should be excluded from results.
	/// </summary>
	public DateTime ValidUntilUtc { get; private set; }

	/// <summary>
	/// UTC timestamp of the last successful sync that touched this record.
	/// </summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>
	/// Short human-readable title of the road work.
	/// </summary>
	public string Title { get; private set; } = default!;

	/// <summary>
	/// Short name of the data source that provided this road work (e.g. "gipod", "osm").
	/// </summary>
	public string Source { get; private set; } = default!;

	/// <summary>
	/// External identifier from the source dataset (e.g. "gipod:98765" or "osm:12345678").
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
	private RoadWork() { }

	/// <summary>
	/// Initialises a fully-populated <see cref="RoadWork"/> instance.
	/// </summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="latitude">WGS84 latitude.</param>
	/// <param name="longitude">WGS84 longitude.</param>
	/// <param name="title">Short title.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="validFromUtc">Start of the active window.</param>
	/// <param name="validUntilUtc">End of the active window.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private RoadWork(RoadWorkId id, string externalId, string source, string countryCode, double latitude, double longitude, string title, string? description, DateTime validFromUtc, DateTime validUntilUtc, DateTime lastSyncedUtc) : base(id)
	{
		Title = title;
		Source = source;
		Latitude = latitude;
		Longitude = longitude;
		ExternalId = externalId;
		CountryCode = countryCode;
		Description = description;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="RoadWork"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is null or empty.
	/// </summary>
	/// <param name="externalId">Source dataset identifier. Must not be null or whitespace.</param>
	/// <param name="source">Short source name. Must not be null or whitespace.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code. Must not be null or whitespace.</param>
	/// <param name="latitude">WGS84 latitude of the road work location.</param>
	/// <param name="longitude">WGS84 longitude of the road work location.</param>
	/// <param name="title">Short human-readable title. Must not be null or whitespace.</param>
	/// <param name="description">Optional extended description.</param>
	/// <param name="validFromUtc">UTC start of the active window.</param>
	/// <param name="validUntilUtc">UTC end of the active window.</param>
	/// <returns>A <see cref="Result{RoadWork}"/> containing the new aggregate, or a failure with an error.</returns>
	public static Result<RoadWork> Create(string externalId, string source, string countryCode, double latitude, double longitude, string title, string? description, DateTime validFromUtc, DateTime validUntilUtc)
	{
		if (string.IsNullOrWhiteSpace(externalId))
			return Result.Failure<RoadWork>(new Error("RoadWork.ExternalId.Required", "ExternalId is required."));

		if (string.IsNullOrWhiteSpace(source))
			return Result.Failure<RoadWork>(new Error("RoadWork.Source.Required", "Source is required."));

		if (string.IsNullOrWhiteSpace(countryCode))
			return Result.Failure<RoadWork>(new Error("RoadWork.CountryCode.Required", "CountryCode is required."));

		if (string.IsNullOrWhiteSpace(title))
			return Result.Failure<RoadWork>(new Error("RoadWork.Title.Required", "Title is required."));

		var now = DateTime.UtcNow;
		var roadWork = new RoadWork(RoadWorkId.New(), externalId, source, countryCode.ToUpperInvariant(), latitude, longitude, title, description, validFromUtc, validUntilUtc, now);
		roadWork.InitializeAudit(now, source);
		return Result.Success(roadWork);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates mutable fields when the Worker re-syncs an existing road work.
	/// ExternalId, Source, and CountryCode are immutable after creation.
	/// </summary>
	/// <param name="latitude">Updated WGS84 latitude.</param>
	/// <param name="longitude">Updated WGS84 longitude.</param>
	/// <param name="title">Updated title.</param>
	/// <param name="description">Updated optional description.</param>
	/// <param name="validFromUtc">Updated start of the active window.</param>
	/// <param name="validUntilUtc">Updated end of the active window.</param>
	public void Sync(double latitude, double longitude, string title, string? description, DateTime validFromUtc, DateTime validUntilUtc)
	{
		Title = title;
		Latitude = latitude;
		Longitude = longitude;
		Description = description;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source);
	}

	/// <summary>
	/// Returns true if the road work is currently active based on UTC now.
	/// </summary>
	public bool IsActive()
	{
		return DateTime.UtcNow >= ValidFromUtc && DateTime.UtcNow < ValidUntilUtc;
	}
	#endregion
}