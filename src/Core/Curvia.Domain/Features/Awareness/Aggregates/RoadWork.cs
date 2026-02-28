using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;
using Curvia.Domain.Features.Awareness.ValueObjects;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a temporary road-works event relevant to motorcyclists.
///              Has a validity window (ValidFromUtc → ValidUntilUtc) used by the
///              scheduled cleanup job to prune expired records.
///              Lifecycle managed entirely by the Worker sync pipeline.
/// </summary>
public sealed class RoadWork : AggregateRoot<RoadWorkId>
{
	#region Properties
	/// <summary>WGS84 geographic position of the road-works location.</summary>
	public Coordinate Position { get; private set; } = default!;

	/// <summary>Short human-readable title (e.g. "A12 resurfacing - lane 2 closure").</summary>
	public AwarenessTitle Title { get; private set; } = default!;

	/// <summary>Optional extended description.</summary>
	public AwarenessDescription? Description { get; private set; }

	/// <summary>UTC start of the active window.</summary>
	public DateTime ValidFromUtc { get; private set; }

	/// <summary>UTC end of the active window.</summary>
	public DateTime ValidUntilUtc { get; private set; }

	/// <summary>UTC timestamp of the last successful sync that touched this record.</summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>Short name of the data source that provided this road work (e.g. "gipod", "tomtom").</summary>
	public SourceName Source { get; private set; } = default!;

	/// <summary>External identifier from the source dataset (e.g. "gipod:98765").</summary>
	public ExternalId ExternalId { get; private set; } = default!;

	/// <summary>ISO 3166-1 alpha-2 country code (e.g. "BE", "FR").</summary>
	public AwarenessCountryCode CountryCode { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private RoadWork() { }

	/// <summary>Initialises a fully-populated <see cref="RoadWork"/> instance.</summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="position">WGS84 position.</param>
	/// <param name="title">Short title.</param>
	/// <param name="description">Optional description.</param>
	/// <param name="validFromUtc">Start of the active window.</param>
	/// <param name="validUntilUtc">End of the active window.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private RoadWork(RoadWorkId id, ExternalId externalId, SourceName source, AwarenessCountryCode countryCode, Coordinate position, AwarenessTitle title, AwarenessDescription? description, DateTime validFromUtc, DateTime validUntilUtc, DateTime lastSyncedUtc) : base(id)
	{
		Title = title;
		Source = source;
		Position = position;
		ExternalId = externalId;
		Description = description;
		CountryCode = countryCode;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="RoadWork"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is invalid.
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
		var externalIdResult = ExternalId.Create(externalId);
		if (externalIdResult.IsFailure)
			return Result.Failure<RoadWork>(externalIdResult.Error);

		var sourceResult = SourceName.Create(source);
		if (sourceResult.IsFailure)
			return Result.Failure<RoadWork>(sourceResult.Error);

		var countryCodeResult = AwarenessCountryCode.Create(countryCode);
		if (countryCodeResult.IsFailure)
			return Result.Failure<RoadWork>(countryCodeResult.Error);

		var positionResult = Coordinate.Create(latitude, longitude);
		if (positionResult.IsFailure)
			return Result.Failure<RoadWork>(positionResult.Error);

		var titleResult = AwarenessTitle.Create(title);
		if (titleResult.IsFailure)
			return Result.Failure<RoadWork>(titleResult.Error);

		var descriptionResult = AwarenessDescription.CreateOptional(description);
		if (descriptionResult.IsFailure)
			return Result.Failure<RoadWork>(descriptionResult.Error);

		var now = DateTime.UtcNow;
		var roadWork = new RoadWork(RoadWorkId.New(), externalIdResult.Value, sourceResult.Value, countryCodeResult.Value, positionResult.Value, titleResult.Value, descriptionResult.Value, validFromUtc, validUntilUtc, now);
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
	/// <returns>Success when updated; otherwise failure.</returns>
	public Result Sync(double latitude, double longitude, string title, string? description, DateTime validFromUtc, DateTime validUntilUtc)
	{
		var positionResult = Coordinate.Create(latitude, longitude);
		if (positionResult.IsFailure)
			return Result.Failure(positionResult.Error);

		var titleResult = AwarenessTitle.Create(title);
		if (titleResult.IsFailure)
			return Result.Failure(titleResult.Error);

		var descriptionResult = AwarenessDescription.CreateOptional(description);
		if (descriptionResult.IsFailure)
			return Result.Failure(descriptionResult.Error);

		Title = titleResult.Value;
		Position = positionResult.Value;
		Description = descriptionResult.Value;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source.Value);
		return Result.Success();
	}

	/// <summary>
	/// Returns true if the road work is currently active based on UTC now.
	/// </summary>
	public bool IsActive() => DateTime.UtcNow >= ValidFromUtc && DateTime.UtcNow < ValidUntilUtc;
	#endregion
}