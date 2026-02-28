using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Domain.Shared.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Immutable WGS84 bounding box — a pure geographic primitive with no feature-specific
///              semantics. Replaces the former duplicate implementations in:
///                - Curvia.Domain.Features.Routing.Routes.ValueObjects
///                - Curvia.Domain.Features.Awareness.ValueObjects
///
///              Property naming follows the geographic standard (South/West/North/East) used by
///              GeoJSON, Overpass QL, and Leaflet — unambiguous regardless of hemisphere.
///
///              Invariants enforced in Create():
///                - All coordinates are finite (no NaN / ±Infinity).
///                - Latitudes within [-90, 90], longitudes within [-180, 180].
///                - South ≤ North, West ≤ East.
///                - Zero-area boxes (collapsed to a line or a point) are rejected unless
///                  allowZeroArea = true — useful when a route computes to a single point.
///                - Anti-meridian-crossing boxes are not supported in v1.
///
///              Area cap is NOT enforced here — it belongs in query handlers because the cap
///              differs per call site (public endpoint vs route corridor).
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

	private BoundingBox() { }

	private BoundingBox(double south, double west, double north, double east)
	{
		South = south;
		West = west;
		North = north;
		East = east;
	}

	#endregion

	#region Factory

	/// <summary>
	/// Creates a validated bounding box from explicit boundary values.
	/// </summary>
	/// <param name="south">Southern latitude boundary (min latitude).</param>
	/// <param name="west">Western longitude boundary (min longitude).</param>
	/// <param name="north">Northern latitude boundary (max latitude).</param>
	/// <param name="east">Eastern longitude boundary (max longitude).</param>
	/// <param name="allowZeroArea">
	/// When true, allows a box collapsed to a line (south == north or west == east).
	/// Useful when a route degenerates to a single point. Defaults to false.
	/// </param>
	public static Result<BoundingBox> Create(
		double south,
		double west,
		double north,
		double east,
		bool allowZeroArea = false)
	{
		if (!IsFinite(south) || !IsFinite(west) || !IsFinite(north) || !IsFinite(east))
			return Result.Failure<BoundingBox>(new Error("BoundingBox.NonFiniteNumber",
				"All BoundingBox coordinates must be finite numbers (no NaN or Infinity)."));

		if (south is < -90.0 or > 90.0)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidSouth",
				$"South latitude {south} is out of range [-90, 90]."));

		if (north is < -90.0 or > 90.0)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidNorth",
				$"North latitude {north} is out of range [-90, 90]."));

		if (west is < -180.0 or > 180.0)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidWest",
				$"West longitude {west} is out of range [-180, 180]."));

		if (east is < -180.0 or > 180.0)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.InvalidEast",
				$"East longitude {east} is out of range [-180, 180]."));

		if (south > north)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.Inverted",
				$"South ({south}) must be ≤ North ({north}). Anti-meridian-crossing boxes are not supported."));

		if (west > east)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.Inverted",
				$"West ({west}) must be ≤ East ({east}). Anti-meridian-crossing boxes are not supported."));

		if (!allowZeroArea && (south == north || west == east))
			return Result.Failure<BoundingBox>(new Error("BoundingBox.ZeroArea",
				"BoundingBox must have a nonzero area. Pass allowZeroArea = true to permit degenerate boxes."));

		return Result.Success(new BoundingBox(south, west, north, east));
	}

	/// <summary>
	/// Derives a bounding box that exactly encloses a collection of geo-coordinates.
	/// </summary>
	/// <param name="points">Non-empty collection of points to enclose.</param>
	/// <param name="allowZeroArea">Forwarded to <see cref="Create"/>.</param>
	public static Result<BoundingBox> FromPoints(
		IReadOnlyCollection<GeoCoordinate> points,
		bool allowZeroArea = false)
	{
		if (points is null)
			return Result.Failure<BoundingBox>(Error.NullValue);

		if (points.Count == 0)
			return Result.Failure<BoundingBox>(new Error("BoundingBox.EmptyPoints",
				"Cannot derive a bounding box from an empty point collection."));

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

	/// <summary>
	/// Derives a bounding box that encloses a polyline expanded by a buffer in metres.
	/// Used for route-corridor awareness queries.
	/// </summary>
	/// <param name="points">Ordered sequence of (Lat, Lon) pairs forming the polyline.</param>
	/// <param name="bufferMeters">Expansion distance in metres, applied on all sides.</param>
	public static Result<BoundingBox> CreateFromPolylineWithBuffer(
		IEnumerable<(double Lat, double Lon)> points,
		double bufferMeters)
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

	#region Methods

	/// <summary>Returns true when <paramref name="point"/> falls within or on the boundary of this box.</summary>
	public bool Contains(GeoCoordinate point)
		=> point.Latitude >= South
		&& point.Latitude <= North
		&& point.Longitude >= West
		&& point.Longitude <= East;

	/// <summary>Returns the geographic center of the bounding box as a <see cref="GeoCoordinate"/>.</summary>
	public GeoCoordinate Center()
	{
		var centerLat = (South + North) / 2.0;
		var centerLon = (West + East) / 2.0;
		var center = GeoCoordinate.Create(centerLat, centerLon);
		return center.IsSuccess
			? center.Value
			: throw new InvalidOperationException("BoundingBox center computation produced an invalid coordinate.");
	}

	/// <summary>Returns the bounding box in Overpass QL format: (south,west,north,east).</summary>
	public string ToOverpassBbox()
		=> $"{South:F6},{West:F6},{North:F6},{East:F6}";

	#endregion

	#region Equality

	protected override bool EqualsCore(BoundingBox other)
		=> South.Equals(other.South)
		&& West.Equals(other.West)
		&& North.Equals(other.North)
		&& East.Equals(other.East);

	protected override int GetHashCodeCore()
		=> HashCode.Combine(South, West, North, East);

	#endregion

	#region Overrides

	public override string ToString()
		=> $"BBox([{South:F6},{West:F6}] → [{North:F6},{East:F6}])";

	#endregion

	#region Helpers

	private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

	#endregion
}