using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a fixed speed camera sourced from OSM or OpenSpeedCam.
///
///              Lifecycle is managed exclusively by the Worker sync pipeline:
///                - Created / updated via upsert during a sync run.
///                - Deleted via bulk delete before a full country reload.
///              The domain has no user-facing write operations for this type.
///
///              ExternalId + Source form a composite natural key within a country.
///              This allows the Worker to upsert without needing to round-trip
///              every record by our internal GUID.
///
///              CountryCode is ISO 3166-1 alpha-2 (e.g. "BE", "FR", "DE").
/// </summary>
public sealed class SpeedCamera : AggregateRoot<SpeedCameraId>
{
	#region Properties
	/// <summary>
	/// WGS84 latitude of the camera position.
	/// </summary>
	public double Latitude { get; private set; }

	/// <summary>
	/// WGS84 longitude of the camera position.
	/// </summary>
	public double Longitude { get; private set; }

	/// <summary>
	/// Speed limit enforced by this camera in km/h, or null when not specified in the source data.
	/// </summary>
	public int? SpeedLimitKmh { get; private set; }

	/// <summary>
	/// UTC timestamp of the last successful sync that touched this record.
	/// </summary>
	public DateTime LastSyncedUtc { get; private set; }

	/// <summary>
	/// Direction the camera faces (cardinal direction or Unknown).
	/// </summary>
	public CameraDirection Direction { get; private set; }

	/// <summary>
	/// Short name of the data source that provided this camera (e.g. "osm", "openspeedcam").
	/// </summary>
	public string Source { get; private set; } = default!;

	/// <summary>
	/// External identifier from the source dataset (e.g. "osm:node/12345678").
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
	private SpeedCamera() { }

	/// <summary>
	/// Initialises a fully-populated <see cref="SpeedCamera"/> instance.
	/// </summary>
	/// <param name="id">Strongly-typed identifier.</param>
	/// <param name="externalId">Source dataset identifier.</param>
	/// <param name="source">Short source name.</param>
	/// <param name="countryCode">ISO country code.</param>
	/// <param name="latitude">WGS84 latitude.</param>
	/// <param name="longitude">WGS84 longitude.</param>
	/// <param name="speedLimitKmh">Optional enforced speed limit in km/h.</param>
	/// <param name="direction">Camera facing direction.</param>
	/// <param name="lastSyncedUtc">UTC timestamp of this sync run.</param>
	private SpeedCamera(SpeedCameraId id, string externalId, string source, string countryCode, double latitude, double longitude, int? speedLimitKmh, CameraDirection direction, DateTime lastSyncedUtc) : base(id)
	{
		Source = source;
		Latitude = latitude;
		Longitude = longitude;
		Direction = direction;
		ExternalId = externalId;
		CountryCode = countryCode;
		SpeedLimitKmh = speedLimitKmh;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="SpeedCamera"/> aggregate from provider-supplied data.
	/// Returns a failure result when any required field is null or empty.
	/// </summary>
	/// <param name="externalId">Source dataset identifier. Must not be null or whitespace.</param>
	/// <param name="source">Short source name. Must not be null or whitespace.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code. Must not be null or whitespace.</param>
	/// <param name="latitude">WGS84 latitude of the camera position.</param>
	/// <param name="longitude">WGS84 longitude of the camera position.</param>
	/// <param name="speedLimitKmh">Optional enforced speed limit in km/h.</param>
	/// <param name="direction">Direction the camera faces.</param>
	/// <returns>A <see cref="Result{SpeedCamera}"/> containing the new aggregate, or a failure with an error.</returns>
	public static Result<SpeedCamera> Create(string externalId, string source, string countryCode, double latitude, double longitude, int? speedLimitKmh, CameraDirection direction)
	{
		if (string.IsNullOrWhiteSpace(externalId))
			return Result.Failure<SpeedCamera>(new Error("SpeedCamera.ExternalId.Required", "ExternalId is required."));

		if (string.IsNullOrWhiteSpace(source))
			return Result.Failure<SpeedCamera>(new Error("SpeedCamera.Source.Required", "Source is required."));

		if (string.IsNullOrWhiteSpace(countryCode))
			return Result.Failure<SpeedCamera>(new Error("SpeedCamera.CountryCode.Required", "CountryCode is required."));

		var now = DateTime.UtcNow;
		var camera = new SpeedCamera(SpeedCameraId.New(), externalId, source, countryCode.ToUpperInvariant(), latitude, longitude, speedLimitKmh, direction, now);
		camera.InitializeAudit(now, source);
		return Result.Success(camera);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates mutable fields when the Worker re-syncs an existing camera.
	/// ExternalId, Source, and CountryCode are immutable after creation.
	/// </summary>
	/// <param name="latitude">Updated WGS84 latitude.</param>
	/// <param name="longitude">Updated WGS84 longitude.</param>
	/// <param name="speedLimitKmh">Updated optional enforced speed limit in km/h.</param>
	/// <param name="direction">Updated camera facing direction.</param>
	public void Sync(double latitude, double longitude, int? speedLimitKmh, CameraDirection direction)
	{
		Latitude = latitude;
		Longitude = longitude;
		SpeedLimitKmh = speedLimitKmh;
		Direction = direction;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source);
	}
	#endregion
}