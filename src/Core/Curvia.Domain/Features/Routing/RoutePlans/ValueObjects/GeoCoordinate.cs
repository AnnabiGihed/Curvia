using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a geographic coordinate (WGS84) using latitude and longitude in degrees.
///              Ensures valid ranges and provides domain-safe creation via Result.
/// </summary>
public sealed class GeoCoordinate : CSharpFunctionalExtensions.ValueObject<GeoCoordinate>
{
	#region Properties

	public double Latitude { get; }
	public double Longitude { get; }

	#endregion

	#region Constructor

	private GeoCoordinate(double latitude, double longitude)
	{
		Latitude = latitude;
		Longitude = longitude;
	}

	#endregion

	#region Factory

	public static Result<GeoCoordinate> Create(double latitude, double longitude)
	{
		if (double.IsNaN(latitude) || double.IsInfinity(latitude))
			return Result.Failure<GeoCoordinate>(RoutingErrors.NonFiniteNumber(nameof(latitude)));

		if (double.IsNaN(longitude) || double.IsInfinity(longitude))
			return Result.Failure<GeoCoordinate>(RoutingErrors.NonFiniteNumber(nameof(longitude)));

		if (latitude is < -90.0 or > 90.0)
			return Result.Failure<GeoCoordinate>(RoutingErrors.InvalidLatitude(latitude));

		if (longitude is < -180.0 or > 180.0)
			return Result.Failure<GeoCoordinate>(RoutingErrors.InvalidLongitude(longitude));

		return Result.Success(new GeoCoordinate(latitude, longitude));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(GeoCoordinate other)
		=> Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);

	protected override int GetHashCodeCore()
		=> HashCode.Combine(Latitude, Longitude);

	#endregion

	#region Overrides

	public override string ToString() => $"{Latitude:F6},{Longitude:F6}";

	#endregion
}
