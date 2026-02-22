namespace Curvia.Infrastructure.Features.Routing.Routes.Helpers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Computes elevation-based fun metrics from Valhalla /height data.
///              Implements the relief scoring formula from the Motorcycle Fun Routing
///              Core design document:
///                ReliefScore = (ElevationGain × 0.6) + (ElevationVariance × 0.4)
///              Both components are normalised per-kilometre and combined into
///              a final score in [0, 1].
/// </summary>
internal static class ElevationAnalyzer
{
	#region Constants

	/// <summary>
	/// Minimum height change between consecutive samples to count as a real gain/loss.
	/// Filters DEM jitter and GPS rounding noise.
	/// </summary>
	private const double MinHeightChangMeters = 1.0;

	/// <summary>
	/// Empirical elevation gain per km that maps to a relief gain-score of 1.0.
	///
	/// CALIBRATION:
	///   Previous: 70 m/km (Grossglockner / Stelvio benchmark).
	///   New:      25 m/km (Ardennes excellent-route benchmark).
	///
	///   Reference routes:
	///     Flat Belgian plateau (Hesbaye):       2–5 m/km gain
	///     Typical Ardennes secondary road:      8–15 m/km gain
	///     Excellent Ardennes loop (Spa area):   15–20 m/km gain
	///     Signal de Botrange approach:          ~20–25 m/km gain
	///     Alpine passes:                        50–80 m/km gain → clamped to 1.0
	/// </summary>
	private const double ElevationGainCeilingPerKm = 25.0;

	/// <summary>
	/// Empirical elevation standard deviation (meters) that maps to a variance score of 1.0.
	///
	/// CALIBRATION:
	///   Previous: 150 m std dev (Alpine benchmark).
	///   New:       80 m std dev (Ardennes excellent-route benchmark).
	///
	///   Reference routes:
	///     Flat Belgian plateau:                 5–15 m std dev
	///     Typical Ardennes road:               30–50 m std dev
	///     Excellent Ardennes loop:             60–80 m std dev
	///     Alpine passes:                       120–200 m std dev → clamped to 1.0
	/// </summary>
	private const double ElevationVarianceCeiling = 80.0;

	#endregion

	#region Public API

	/// <summary>
	/// Computes a normalised elevation score [0.0, 1.0] from an array of height samples.
	/// </summary>
	/// <param name="heights">
	/// Ordered elevation values in meters returned by Valhalla /height.
	/// Minimum 2 values required; fewer returns 0.
	/// </param>
	/// <param name="totalDistanceMeters">Total route distance in meters for per-km normalisation.</param>
	/// <returns>
	/// Elevation score calibrated to North-European (Ardennes) roads:
	///   0.0–0.15 = flat (Brussels suburbs, Hesbaye plateau)
	///   0.2–0.4  = mildly hilly (southern Brabant, Condroz)
	///   0.5–0.7  = good Ardennes hills (Ourthe, Semois valleys)
	///   0.8–1.0  = excellent Ardennes or Alpine (Spa area, Signal de Botrange approach)
	/// </returns>
	public static double ComputeElevationScore(IReadOnlyList<double> heights, double totalDistanceMeters)
	{
		if (heights is null || heights.Count < 2 || totalDistanceMeters < 1.0)
			return 0.0;

		#region Compute elevation gain and variance
		var totalGainMeters = 0.0;

		for (var i = 1; i < heights.Count; i++)
		{
			var delta = heights[i] - heights[i - 1];
			if (Math.Abs(delta) < MinHeightChangMeters) continue;
			if (delta > 0) totalGainMeters += delta;
		}

		var mean = heights.Average();
		var variance = heights.Sum(h => (h - mean) * (h - mean)) / heights.Count;
		var stdDev = Math.Sqrt(variance);
		#endregion

		#region Normalise per-km and combine
		var distanceKm = totalDistanceMeters / 1000.0;
		var gainPerKm = totalGainMeters / distanceKm;
		var gainScore = Math.Min(gainPerKm / ElevationGainCeilingPerKm, 1.0);
		var varianceScore = Math.Min(stdDev / ElevationVarianceCeiling, 1.0);

		// Design doc formula: ReliefScore = (ElevationGain × 0.6) + (ElevationVariance × 0.4)
		var reliefScore = gainScore * 0.6 + varianceScore * 0.4;
		return Math.Max(Math.Min(reliefScore, 1.0), 0.0);
		#endregion
	}

	#endregion
}