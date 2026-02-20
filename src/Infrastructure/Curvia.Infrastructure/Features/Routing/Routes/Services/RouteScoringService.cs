using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Scores Valhalla route candidates for fun motorcycle routing.
///              V1 scoring uses variant-name proxies for fun metrics until real
///              curvature/elevation/scenery data is available from the graph worker.
///
///              Scoring formula (V1):
///                score = weightedFunProxy - timePenalty - distancePenalty
///
///              Hard disqualifications (returns -1_000_000):
///                - MaxDetourRatio exceeded
///                - MaxDurationSeconds exceeded
///
///              Soft penalties:
///                - MaxDistance exceeded (proportional penalty)
///                - Long ride time (0.25 pts/hour bias toward shorter rides)
/// </summary>
internal sealed class RouteScoringService : IRouteScoringService
{
	#region IRouteScoringService
	/// <inheritdoc/>
	public double Score(ValhallaRouteResponse candidate, RoutePlan plan, string variantName)
	{
		#region Guards
		if (candidate?.Trip is null) return double.NegativeInfinity;
		if (plan is null) return double.NegativeInfinity;
		#endregion

		#region Extract Valhalla summary
		var km = candidate.Trip.Summary.Length;
		var routeMeters = km * 1000.0;
		var timeSeconds = candidate.Trip.Summary.Time;
		#endregion

		#region Hard constraint — MaxDurationSeconds
		if (plan.Constraints.MaxDurationSeconds.HasValue && timeSeconds > plan.Constraints.MaxDurationSeconds.Value)
			return -1_000_000;
		#endregion

		#region Hard constraint — MaxDetourRatio
		var baselineMeters = ComputeBaselineMeters(plan, routeMeters);
		var detourRatio = routeMeters / baselineMeters;

		if (detourRatio > plan.Constraints.MaxDetourRatio)
			return -1_000_000;
		#endregion

		#region Soft penalty — MaxDistance
		var distancePenalty = 0.0;
		if (plan.Constraints.MaxDistance is not null)
		{
			var cap = plan.Constraints.MaxDistance.Meters;
			if (routeMeters > cap)
			{
				// Proportional penalty: 250 pts per 100 % overage
				var over = (routeMeters - cap) / cap;
				distancePenalty = 250.0 * over;
			}
		}
		#endregion

		#region Fun proxy (V1)
		// Variant base fun values — reflects how "fun" the Valhalla costing settings
		// are expected to be in terms of road selection.
		// Replace with real curvature/elevation/scenery metrics in V2.
		var variantFun = variantName switch
		{
			"scenic" => 0.90,
			"explorer" => 0.80,
			"balanced" => 0.65,
			_ => 0.50
		};

		var weights = plan.ScoringProfile.Weights;

		// Combine weights with the variant fun proxy
		var weightedFun =weights.Curves * variantFun + 	weights.Scenery * variantFun + weights.Elevation * 0.30; // elevation placeholder (no real DEM data yet)

		// Apply FunFactor: range [0,10] → multiplier [1.0, 2.0]
		var funScore = weightedFun * (1.0 + plan.ScoringProfile.FunFactor / 10.0);
		#endregion

		#region Soft bias — ride duration (prefer shorter rides, all else equal)
		// 0.25 points penalty per hour of riding time
		var timePenalty = timeSeconds / 3600.0 * 0.25;
		#endregion

		#region Final score
		// Higher is better.
		return funScore - timePenalty - distancePenalty;
		#endregion
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Computes the baseline distance used for detour ratio calculation.
	/// For point-to-point: straight-line (Haversine) distance from start to end.
	/// For loop with target: the target distance itself.
	/// Fallback: the actual route distance (ratio = 1, no penalty).
	/// </summary>
	private static double ComputeBaselineMeters(RoutePlan plan, double routeMeters)
	{
		if (plan.End is not null)
		{
			var haversine = HaversineMeters(plan.Start, plan.End);
			return haversine > 1.0 ? haversine : routeMeters;
		}

		if (plan.LoopSpec?.TargetDistance is not null)
			return plan.LoopSpec.TargetDistance.Meters;

		return routeMeters;
	}

	/// <summary>
	/// Computes the great-circle distance between two coordinates using the Haversine formula.
	/// </summary>
	/// <param name="a">First coordinate.</param>
	/// <param name="b">Second coordinate.</param>
	/// <returns>Distance in meters.</returns>
	private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
	{
		const double R = 6_371_000.0;

		var dLat = ToRad(b.Latitude - a.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);
		var lat1 = ToRad(a.Latitude);
		var lat2 = ToRad(b.Latitude);

		var sinDLat = Math.Sin(dLat / 2);
		var sinDLon = Math.Sin(dLon / 2);

		var h = sinDLat * sinDLat + Math.Cos(lat1) * Math.Cos(lat2) * sinDLon * sinDLon;

		return 2 * R * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
	}

	/// <summary>Converts degrees to radians.</summary>
	private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
	#endregion
}