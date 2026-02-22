using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Helpers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Computes road curvature metrics from a decoded route polyline.
///              Implements the curvature algorithm specified in the Motorcycle Fun Routing.
/// </summary>
internal static class RouteGeometryAnalyzer
{
	#region Constants
	/// <summary>
	/// Number of consecutive curve segments required to qualify as a "flowing" curve sequence.
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
	///
	/// CALIBRATION:
	///   Previous value: 350°/km (Stelvio Pass / Alpine benchmark).
	///   This made even the best Wallonian roads score 0.3–0.4, producing
	///   1-star ratings on excellent routes through the Dinant gorge and Durbuy.
	///
	///   New value: 150°/km (Wallonian / North-European benchmark).
	///   Represents the curvature of the most twisty sustained motorcycle roads
	///   accessible in the primary Curvia deployment region (Belgium, Luxembourg,
	///   northern France, western Germany).
	///
	///   Reference roads:
	///     Dinant gorge (N92/N936):       ~120–150°/km
	///     Fonds de Quareux (Ourthe):     ~100–130°/km
	///     Semois valley (N884):          ~90–120°/km
	///     Typical Ardennes secondary:    ~40–80°/km
	///     Routes exceeding 150°/km (Alpine, Pyrenees) → clamped to 1.0 as expected.
	/// </summary>
	private const double CurvatureIndexCeiling = 150.0;

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
	/// Curvature score calibrated to North-European roads:
	///   0.0  = perfectly straight (motorway / flat N-road)
	///   0.3  = mildly winding secondary road
	///   0.6  = good Ardennes secondary road (typical Ourthe/Semois valley)
	///   0.8  = excellent twisty road (Dinant gorge, Fonds de Quareux)
	///   1.0  = 150°/km or above (best regional roads / Alpine standard)
	/// </returns>
	public static double ComputeCurvatureScore(IReadOnlyList<GeoCoordinate> points)
	{
		if (points is null || points.Count < 3)
			return 0.0;

		#region Segment computation
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
			if (lengths[i] < MinSegmentLengthMeters)
			{
				consecutiveCurveCount = 0;
				continue;
			}

			var delta = NormaliseAngle(headings[i + 1] - headings[i]);

			if (delta < MinAngularDeltaDeg)
			{
				consecutiveCurveCount = 0;
				continue;
			}

			consecutiveCurveCount++;

			var flowMultiplier = consecutiveCurveCount >= FlowSequenceThreshold ? FlowBonusMultiplier : 1.0;

			totalWeightedDelta += delta * flowMultiplier;
		}
		#endregion

		#region Normalise to [0, 1]
		var curvatureIndexPerKm = totalWeightedDelta / (totalDistance / 1000.0);
		var score = Math.Min(curvatureIndexPerKm / CurvatureIndexCeiling, 1.0);
		return Math.Max(score, 0.0);
		#endregion
	}
	#endregion

	#region Private — Geometry helpers
	private static double ToRad(double degrees)
	{
		return degrees * Math.PI / 180.0;
	}
	private static double ToDeg(double radians)
	{
		return radians * 180.0 / Math.PI;
	}
	private static double NormaliseAngle(double delta)
	{
		delta = ((delta % 360.0) + 360.0) % 360.0;
		return delta > 180.0 ? 360.0 - delta : delta;
	}

	/// <summary>
	/// Computes initial bearing from point a to point b in degrees [0..360).
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

	/// <summary>
	/// Computes Haversine distance in meters between two points.
	/// </summary>
	private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
	{
		var dLat = ToRad(b.Latitude - a.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);

		var sinDLat = Math.Sin(dLat / 2);
		var sinDLon = Math.Sin(dLon / 2);

		var h = sinDLat * sinDLat +
				Math.Cos(ToRad(a.Latitude)) * Math.Cos(ToRad(b.Latitude)) * sinDLon * sinDLon;

		return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
	}
	#endregion
}