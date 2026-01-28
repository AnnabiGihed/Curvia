using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a geographic bounding box (WGS84) defined by min/max latitude and longitude.
///              Used to constrain routing/awareness queries and spatial lookups.
/// </summary>
public sealed class BoundingBox : CSharpFunctionalExtensions.ValueObject<BoundingBox>
{
	#region Properties

	public double MinLatitude { get; }
	public double MinLongitude { get; }
	public double MaxLatitude { get; }
	public double MaxLongitude { get; }

	#endregion

	#region Constructor
	private BoundingBox()
	{
	}
	private BoundingBox(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude)
	{
		MinLatitude = minLatitude;
		MinLongitude = minLongitude;
		MaxLatitude = maxLatitude;
		MaxLongitude = maxLongitude;
	}

	#endregion

	#region Factory

	public static Result<BoundingBox> Create(
		double minLatitude,
		double minLongitude,
		double maxLatitude,
		double maxLongitude,
		bool allowZeroArea = false)
	{
		if (!IsFinite(minLatitude) || !IsFinite(minLongitude) || !IsFinite(maxLatitude) || !IsFinite(maxLongitude))
			return Result.Failure<BoundingBox>(RoutingErrors.NonFiniteNumber(nameof(BoundingBox)));

		if (minLatitude is < -90.0 or > 90.0)
			return Result.Failure<BoundingBox>(RoutingErrors.InvalidLatitude(minLatitude));
		if (maxLatitude is < -90.0 or > 90.0)
			return Result.Failure<BoundingBox>(RoutingErrors.InvalidLatitude(maxLatitude));

		if (minLongitude is < -180.0 or > 180.0)
			return Result.Failure<BoundingBox>(RoutingErrors.InvalidLongitude(minLongitude));
		if (maxLongitude is < -180.0 or > 180.0)
			return Result.Failure<BoundingBox>(RoutingErrors.InvalidLongitude(maxLongitude));

		if (minLatitude > maxLatitude)
			return Result.Failure<BoundingBox>(RoutingErrors.BoundingBoxInverted($"MinLatitude ({minLatitude}) > MaxLatitude ({maxLatitude})."));

		if (minLongitude > maxLongitude)
			return Result.Failure<BoundingBox>(RoutingErrors.BoundingBoxInverted($"MinLongitude ({minLongitude}) > MaxLongitude ({maxLongitude})."));

		if (!allowZeroArea && (minLatitude == maxLatitude || minLongitude == maxLongitude))
			return Result.Failure<BoundingBox>(RoutingErrors.BoundingBoxTooSmall());

		return Result.Success(new BoundingBox(minLatitude, minLongitude, maxLatitude, maxLongitude));
	}

	public static Result<BoundingBox> FromPoints(IReadOnlyCollection<GeoCoordinate> points, bool allowZeroArea = false)
	{
		if (points is null)
			return Result.Failure<BoundingBox>(Error.NullValue);

		if (points.Count == 0)
			return Result.Failure<BoundingBox>(RoutingErrors.BoundingBoxInvalid());

		var minLat = double.MaxValue;
		var minLon = double.MaxValue;
		var maxLat = double.MinValue;
		var maxLon = double.MinValue;

		foreach (var p in points)
		{
			if (p.Latitude < minLat) minLat = p.Latitude;
			if (p.Longitude < minLon) minLon = p.Longitude;

			if (p.Latitude > maxLat) maxLat = p.Latitude;
			if (p.Longitude > maxLon) maxLon = p.Longitude;
		}

		return Create(minLat, minLon, maxLat, maxLon, allowZeroArea);
	}

	private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

	#endregion

	#region Methods

	public bool Contains(GeoCoordinate point)
		=> point.Latitude >= MinLatitude
		   && point.Latitude <= MaxLatitude
		   && point.Longitude >= MinLongitude
		   && point.Longitude <= MaxLongitude;

	public GeoCoordinate Center()
	{
		// Center is always valid because bounds are already validated.
		var centerLat = (MinLatitude + MaxLatitude) / 2.0;
		var centerLon = (MinLongitude + MaxLongitude) / 2.0;

		// Create(...) cannot fail given validated inputs; still keep domain safety.
		var center = GeoCoordinate.Create(centerLat, centerLon);
		return center.IsSuccess ? center.Value : throw new InvalidOperationException("BoundingBox center computation produced invalid coordinate.");
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(BoundingBox other)
		=> MinLatitude.Equals(other.MinLatitude)
		   && MinLongitude.Equals(other.MinLongitude)
		   && MaxLatitude.Equals(other.MaxLatitude)
		   && MaxLongitude.Equals(other.MaxLongitude);

	protected override int GetHashCodeCore()
		=> HashCode.Combine(MinLatitude, MinLongitude, MaxLatitude, MaxLongitude);

	#endregion

	#region Overrides

	public override string ToString()
		=> $"BBox([{MinLatitude:F6},{MinLongitude:F6}] -> [{MaxLatitude:F6},{MaxLongitude:F6}])";

	#endregion
}
