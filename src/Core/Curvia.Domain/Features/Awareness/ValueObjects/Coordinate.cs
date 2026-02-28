using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : WGS84 geographic coordinate (latitude / longitude pair) for awareness entities.
///              Separate from <c>Curvia.Domain.Features.Routing.Shared.GeoCoordinate</c> to
///              avoid a cross-feature domain dependency.  Latitude and longitude are validated
///              against WGS84 bounds on construction.
/// </summary>
public sealed class Coordinate : CSharpFunctionalExtensions.ValueObject<Coordinate>
{
	#region Properties
	/// <summary>WGS84 latitude in decimal degrees. Valid range: −90 to +90.</summary>
	public double Latitude { get; }

	/// <summary>WGS84 longitude in decimal degrees. Valid range: −180 to +180.</summary>
	public double Longitude { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private Coordinate() { }

	/// <summary>Initialises a <see cref="Coordinate"/> with validated values.</summary>
	/// <param name="latitude">WGS84 latitude.</param>
	/// <param name="longitude">WGS84 longitude.</param>
	private Coordinate(double latitude, double longitude)
	{
		Latitude = latitude;
		Longitude = longitude;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="Coordinate"/>.
	/// </summary>
	/// <param name="latitude">WGS84 latitude (−90 to +90).</param>
	/// <param name="longitude">WGS84 longitude (−180 to +180).</param>
	/// <returns>Successful result when both values are in range; otherwise failure.</returns>
	public static Result<Coordinate> Create(double latitude, double longitude)
	{
		if (latitude < -90.0 || latitude > 90.0)
			return Result.Failure<Coordinate>(new Error("Coordinate.Latitude.OutOfRange", $"Latitude '{latitude}' must be between -90 and 90."));

		if (longitude < -180.0 || longitude > 180.0)
			return Result.Failure<Coordinate>(new Error("Coordinate.Longitude.OutOfRange", $"Longitude '{longitude}' must be between -180 and 180."));

		return Result.Success(new Coordinate(latitude, longitude));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="latitude">Stored latitude value.</param>
	/// <param name="longitude">Stored longitude value.</param>
	/// <returns>A <see cref="Coordinate"/> instance without validation.</returns>
	public static Coordinate FromPersistence(double latitude, double longitude) => new(latitude, longitude);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(Coordinate other) =>
		Math.Abs(Latitude - other.Latitude) < 1e-9 &&
		Math.Abs(Longitude - other.Longitude) < 1e-9;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => HashCode.Combine(Latitude, Longitude);
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
	#endregion
}