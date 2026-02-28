using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Immutable WGS84 bounding box used as a spatial filter for awareness queries.
///              Extends <see cref="ValueObject{T}"/> — BoundingBox has no identity.
///              Two instances with identical coordinates represent the same bounding box.
///
///              Invariants enforced in Create():
///                - All coordinates within valid WGS84 ranges.
///                - South strictly less than North.
///                - West strictly less than East.
///                  (Anti-meridian-crossing boxes not supported in v1.)
///
///              Area cap is NOT enforced here — it belongs in the query handler
///              because the cap differs per call site (public endpoint vs route corridor).
/// </summary>
public sealed class BoundingBox : CSharpFunctionalExtensions.ValueObject<BoundingBox>
{
	#region Properties
	public double South { get; }
	public double West { get; }
	public double North { get; }
	public double East { get; }

	/// <summary>
	/// Approximate area in square degrees. Used by handlers to enforce query caps.
	/// </summary>
	public double ApproxAreaDegrees => (North - South) * (East - West);
	#endregion

	#region Constructor
	private BoundingBox(double south, double west, double north, double east)
	{
		South = south;
		West = west;
		North = north;
		East = east;
	}
	#endregion

	#region Factory
	public static Result<BoundingBox> Create(double south, double west, double north, double east)
	{
		if (south < -90 || south > 90)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidSouth",
				$"South latitude {south} is out of range [-90, 90]."));

		if (north < -90 || north > 90)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidNorth",
				$"North latitude {north} is out of range [-90, 90]."));

		if (west < -180 || west > 180)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidWest",
				$"West longitude {west} is out of range [-180, 180]."));

		if (east < -180 || east > 180)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidEast",
				$"East longitude {east} is out of range [-180, 180]."));

		if (south >= north)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidLatitudeOrder",
				"South latitude must be strictly less than North latitude."));

		if (west >= east)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidLongitudeOrder",
				"West longitude must be strictly less than East longitude. "
			  + "Anti-meridian-crossing boxes are not supported."));

		return Result.Success(new BoundingBox(south, west, north, east));
	}

	/// <summary>
	/// Derives a bounding box that encloses a polyline expanded by a buffer in metres.
	/// Used for route-corridor awareness queries.
	/// </summary>
	public static Result<BoundingBox> CreateFromPolylineWithBuffer(
		IEnumerable<(double Lat, double Lon)> points, double bufferMeters)
	{
		var list = points.ToList();
		if (list.Count == 0)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.EmptyPolyline",
				"Cannot derive a bounding box from an empty polyline."));

		var minLat = list.Min(p => p.Lat);
		var maxLat = list.Max(p => p.Lat);
		var minLon = list.Min(p => p.Lon);
		var maxLon = list.Max(p => p.Lon);

		const double MetersPerDegree = 111_000.0;
		var latDelta = bufferMeters / MetersPerDegree;
		var midLat = (minLat + maxLat) / 2.0;
		var lonDelta = bufferMeters / (MetersPerDegree * Math.Cos(midLat * Math.PI / 180.0));

		return Create(
			south: Math.Max(-90, minLat - latDelta),
			west: Math.Max(-180, minLon - lonDelta),
			north: Math.Min(90, maxLat + latDelta),
			east: Math.Min(180, maxLon + lonDelta));
	}
	#endregion

	#region ValueObject<BoundingBox>
	protected override bool EqualsCore(BoundingBox other)
		=> South == other.South
		&& West == other.West
		&& North == other.North
		&& East == other.East;

	protected override int GetHashCodeCore()
		=> HashCode.Combine(South, West, North, East);
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the bounding box in Overpass QL format: (south,west,north,east).
	/// </summary>
	public string ToOverpassBbox()
		=> $"{South:F6},{West:F6},{North:F6},{East:F6}";

	public override string ToString()
		=> $"[{South:F4},{West:F4} → {North:F4},{East:F4}]";
	#endregion
}