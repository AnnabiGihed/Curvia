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
	/// Minimum height change in meters between consecutive samples to count as
	/// a real gain/loss (filters GPS/DEM jitter).
	/// </summary>
	private const double MinHeightChangMeters = 1.0;

	/// <summary>
	/// Empirical elevation gain per km that maps to a relief score of 1.0.
	/// Alpine passes (e.g. Stelvio, Grossglockner) reach ~80 m/km gain.
	/// 70 m/km is a realistic ceiling for "perfect" mountain routes in Europe.
	/// </summary>
	private const double ElevationGainCeilingPerKm = 70.0;

	/// <summary>
	/// Empirical elevation variance (std dev, meters) that maps to a variance score of 1.0.
	/// Very hilly routes in the Ardennes or Vosges reach ~150 m std dev.
	/// </summary>
	private const double ElevationVarianceCeiling = 150.0;
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
	/// Elevation score where 0.0 = completely flat road and 1.0 = extreme alpine profile.
	/// </returns>
	public static double ComputeElevationScore(IReadOnlyList<double> heights, double totalDistanceMeters)
	{
		if (heights is null || heights.Count < 2 || totalDistanceMeters < 1.0)
			return 0.0;

		#region Compute elevation gain and variance
		var totalGainMeters = 0.0;
		var totalLossMeters = 0.0;

		for (var i = 1; i < heights.Count; i++)
		{
			var delta = heights[i] - heights[i - 1];

			if (Math.Abs(delta) < MinHeightChangMeters)
				continue;

			if (delta > 0)
				totalGainMeters += delta;
			else
				totalLossMeters += Math.Abs(delta);
		}

		// Elevation variance (standard deviation) as a measure of terrain drama
		var mean = heights.Average();
		var variance = heights.Sum(h => (h - mean) * (h - mean)) / heights.Count;
		var stdDev = Math.Sqrt(variance);
		#endregion

		#region Normalise per-km and combine
		var distanceKm = totalDistanceMeters / 1000.0;
		var gainPerKm = totalGainMeters / distanceKm;

		var gainScore = Math.Min(gainPerKm / ElevationGainCeilingPerKm, 1.0);
		var varianceScore = Math.Min(stdDev / ElevationVarianceCeiling, 1.0);

		// Design doc formula: ReliefScore = (gain × 0.6) + (variance × 0.4)
		var reliefScore = gainScore * 0.6 + varianceScore * 0.4;

		return Math.Max(Math.Min(reliefScore, 1.0), 0.0);
		#endregion
	}
	#endregion
}