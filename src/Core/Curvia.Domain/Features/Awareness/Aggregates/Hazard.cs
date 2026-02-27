using Curvia.Domain.Features.Awareness.Enums;
using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a permanent road hazard relevant to motorcyclists.
///              Extends <see cref="Entity{TId}"/>, not AggregateRoot — permanent hazards
///              are static reference data with no domain events or aggregate invariants.
///              Lifecycle managed entirely by the Worker sync pipeline.
/// </summary>
public sealed class Hazard : AggregateRoot<HazardId>
{
	#region Properties
	public double Latitude { get; private set; }
	public double Longitude { get; private set; }
	public string? Description { get; private set; }
	public HazardType HazardType { get; private set; }
	public DateTime LastSyncedUtc { get; private set; }
	public string Source { get; private set; } = default!;
	public string ExternalId { get; private set; } = default!;
	public string CountryCode { get; private set; } = default!;
	#endregion

	#region Constructor
	private Hazard() { }

	private Hazard(
		HazardId id,
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		HazardType hazardType,
		string? description,
		DateTime lastSyncedUtc)
		: base(id)
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
	public static Hazard Create(
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		HazardType hazardType,
		string? description)
	{
		var now = DateTime.UtcNow;
		var hazard = new Hazard(
			HazardId.New(),
			externalId,
			source,
			countryCode.ToUpperInvariant(),
			latitude,
			longitude,
			hazardType,
			description,
			now);

		hazard.InitializeAudit(now, source);
		return hazard;
	}
	#endregion

	#region Domain behaviour
	public void Sync(
		double latitude,
		double longitude,
		HazardType hazardType,
		string? description)
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