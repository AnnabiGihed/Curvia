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
	public string ExternalId { get; private set; } = default!;
	public string Source { get; private set; } = default!;
	public string CountryCode { get; private set; } = default!;
	public double Latitude { get; private set; }
	public double Longitude { get; private set; }
	public int? SpeedLimitKmh { get; private set; }
	public CameraDirection Direction { get; private set; }
	public DateTime LastSyncedUtc { get; private set; }
	#endregion

	#region Constructor
	private SpeedCamera() { }

	private SpeedCamera(
		SpeedCameraId id,
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		int? speedLimitKmh,
		CameraDirection direction,
		DateTime lastSyncedUtc)
		: base(id)
	{
		ExternalId = externalId;
		Source = source;
		CountryCode = countryCode;
		Latitude = latitude;
		Longitude = longitude;
		SpeedLimitKmh = speedLimitKmh;
		Direction = direction;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	public static SpeedCamera Create(
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		int? speedLimitKmh,
		CameraDirection direction)
	{
		var now = DateTime.UtcNow;
		var camera = new SpeedCamera(
			SpeedCameraId.New(),
			externalId,
			source,
			countryCode.ToUpperInvariant(),
			latitude,
			longitude,
			speedLimitKmh,
			direction,
			now);

		camera.InitializeAudit(now, source);
		return camera;
	}
	#endregion

	#region Domain behaviour
	/// <summary>
	/// Updates mutable fields when the Worker re-syncs an existing camera.
	/// ExternalId, Source, and CountryCode are immutable after creation.
	/// </summary>
	public void Sync(
		double latitude,
		double longitude,
		int? speedLimitKmh,
		CameraDirection direction)
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