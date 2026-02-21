using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Helpers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Computes road curvature metrics from a decoded route polyline.
///              Implements the curvature algorithm specified in the Motorcycle Fun Routing
///              Core design document:
///                1. Compute heading for each segment
///                2. Compute angular delta between consecutive headings
///                3. Filter noise (delta &lt; 5°) and micro-zigzags (segment &lt; 20 m)
///                4. Apply flow detection: sustained curve sequences score higher
///                5. Normalise to [0, 1] using empirical road benchmarks
/// </summary>
internal static class RouteGeometryAnalyzer
{
	#region Constants
	/// <summary>
	/// Number of consecutive curve segments required to qualify as a "flowing" curve sequence.
	/// Flowing sequences receive a bonus multiplier.
	/// </summary>
	private const int FlowSequenceThreshold = 3;

	/// <summary>Heading change below this threshold is treated as GPS noise and ignored.</summary>
	private const double MinAngularDeltaDeg = 5.0;

	/// <summary>Multiplier applied to angular deltas that form part of a sustained flow sequence.</summary>
	private const double FlowBonusMultiplier = 1.25;

	/// <summary>Segments shorter than this are ignored to avoid urban micro-zigzag inflation.</summary>
	private const double MinSegmentLengthMeters = 20.0;

	/// <summary>
	/// Empirical curvature index (°/km) that maps to a score of 1.0.
	/// Roads above the Stelvio Pass in the Alps reach ~400°/km.
	/// 350°/km is a realistic ceiling for "perfect" twisty routes in Europe.
	/// </summary>
	private const double CurvatureIndexCeiling = 350.0;

	/// <summary>Earth's mean radius in meters for Haversine calculations.</summary>
	private const double EarthRadiusMeters = 6_371_000.0;
	#endregion

	#region Public API
	/// <summary>
	/// Computes a normalised curvature score [0.0, 1.0] for the given route polyline.
	/// </summary>
	/// <param name="points">
	/// Ordered sequence of decoded route coordinates.
	/// Minimum 3 points required; fewer points returns 0.
	/// </param>
	/// <returns>
	/// Curvature score where 0.0 = perfectly straight road and 1.0 = extremely twisty
	/// (equivalent to ~350°/km angular deviation rate).
	/// </returns>
	public static double ComputeCurvatureScore(IReadOnlyList<GeoCoordinate> points)
	{
		if (points is null || points.Count < 3)
			return 0.0;

		#region Segment computation
		// Pre-compute segment lengths and headings for each consecutive pair
		var segmentCount = points.Count - 1;
		var lengths = new double[segmentCount];
		var headings = new double[segmentCount];

		for (var i = 0; i < segmentCount; i++)
		{
			lengths[i] = HaversineMeters(points[i], points[i + 1]);
			headings[i] = Bearing(points[i], points[i + 1]);
		}
		#endregion

		#region Angular delta + flow detection
		double totalWeightedDelta = 0.0;
		double totalDistance = lengths.Sum();

		if (totalDistance < 1.0)
			return 0.0;

		var consecutiveCurveCount = 0;

		for (var i = 0; i < segmentCount - 1; i++)
		{
			// Skip segments too short to be meaningful
			if (lengths[i] < MinSegmentLengthMeters)
			{
				consecutiveCurveCount = 0;
				continue;
			}

			var delta = NormaliseAngle(headings[i + 1] - headings[i]);

			// Skip GPS noise
			if (delta < MinAngularDeltaDeg)
			{
				consecutiveCurveCount = 0;
				continue;
			}

			// Accumulate consecutive curve count for flow detection
			consecutiveCurveCount++;

			// Apply flow bonus for sustained curve sequences
			var flowMultiplier = consecutiveCurveCount >= FlowSequenceThreshold
				? FlowBonusMultiplier
				: 1.0;

			totalWeightedDelta += delta * flowMultiplier;
		}
		#endregion

		#region Normalise to [0, 1]
		// Curvature index: weighted angular change per kilometre
		var curvatureIndexPerKm = totalWeightedDelta / (totalDistance / 1000.0);

		// Clamp and normalise against the empirical ceiling
		var score = Math.Min(curvatureIndexPerKm / CurvatureIndexCeiling, 1.0);

		return Math.Max(score, 0.0);
		#endregion
	}
	#endregion

	#region Private — Geometry helpers
	/// <summary>
	/// Normalises an angular difference to the range [0, 180] degrees.
	/// A heading change of 200° clockwise is equivalent to 160° counter-clockwise.
	/// </summary>
	private static double NormaliseAngle(double delta)
	{
		delta = ((delta % 360.0) + 360.0) % 360.0;
		return delta > 180.0 ? 360.0 - delta : delta;
	}

	/// <summary>
	/// Computes the initial compass bearing in degrees [0, 360) from point <paramref name="a"/>
	/// to point <paramref name="b"/> using the spherical Earth model.
	/// </summary>
	private static double Bearing(GeoCoordinate a, GeoCoordinate b)
	{
		var lat1 = ToRad(a.Latitude);
		var lat2 = ToRad(b.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);

		var y = Math.Sin(dLon) * Math.Cos(lat2);
		var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

		return (ToDeg(Math.Atan2(y, x)) + 360.0) % 360.0;
	}
	private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
	private static double ToDeg(double radians) => radians * 180.0 / Math.PI;

	/// <summary>
	/// Computes the great-circle distance in meters between two coordinates (Haversine formula).
	/// </summary>
	private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
	{
		var dLat = ToRad(b.Latitude - a.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);
		var lat1 = ToRad(a.Latitude);
		var lat2 = ToRad(b.Latitude);

		var sinDLat = Math.Sin(dLat / 2);
		var sinDLon = Math.Sin(dLon / 2);
		var h = sinDLat * sinDLat + Math.Cos(lat1) * Math.Cos(lat2) * sinDLon * sinDLon;

		return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
	}
	#endregion
}