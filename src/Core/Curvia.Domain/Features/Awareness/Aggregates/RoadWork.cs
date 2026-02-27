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
	public string ExternalId { get; private set; } = default!;
	public string Source { get; private set; } = default!;
	public string CountryCode { get; private set; } = default!;
	public double Latitude { get; private set; }
	public double Longitude { get; private set; }
	public string Title { get; private set; } = default!;
	public string? Description { get; private set; }
	public DateTime ValidFromUtc { get; private set; }
	public DateTime ValidUntilUtc { get; private set; }
	public DateTime LastSyncedUtc { get; private set; }
	#endregion

	#region Constructor
	private RoadWork() { }

	private RoadWork(
		RoadWorkId id,
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		string title,
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
		Title = title;
		Description = description;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = lastSyncedUtc;
	}
	#endregion

	#region Factory
	public static RoadWork Create(
		string externalId,
		string source,
		string countryCode,
		double latitude,
		double longitude,
		string title,
		string? description,
		DateTime validFromUtc,
		DateTime validUntilUtc)
	{
		var now = DateTime.UtcNow;
		var roadWork = new RoadWork(
			RoadWorkId.New(),
			externalId,
			source,
			countryCode.ToUpperInvariant(),
			latitude,
			longitude,
			title,
			description,
			validFromUtc,
			validUntilUtc,
			now);

		roadWork.InitializeAudit(now, source);
		return roadWork;
	}
	#endregion

	#region Domain behaviour
	public void Sync(
		double latitude,
		double longitude,
		string title,
		string? description,
		DateTime validFromUtc,
		DateTime validUntilUtc)
	{
		Latitude = latitude;
		Longitude = longitude;
		Title = title;
		Description = description;
		ValidFromUtc = validFromUtc;
		ValidUntilUtc = validUntilUtc;
		LastSyncedUtc = DateTime.UtcNow;
		Touch(LastSyncedUtc, Source);
	}

	/// <summary>Returns true if the road work is currently active based on UTC now.</summary>
	public bool IsActive()
	{
		return DateTime.UtcNow >= ValidFromUtc && DateTime.UtcNow < ValidUntilUtc;
	}
	#endregion
}