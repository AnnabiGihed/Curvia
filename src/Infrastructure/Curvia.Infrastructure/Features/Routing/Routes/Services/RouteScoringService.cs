using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

internal sealed class RouteScoringService : IRouteScoringService
{
	/// <summary>
	/// Author      : Gihed Annabi
	/// Date        : 01-2026
	/// Purpose     : Scores Valhalla route candidates for motorcycle routing.
	///              V1 uses:
	///              - distance/time from Valhalla summary
	///              - constraint penalties (detour ratio, max distance)
	///              - a fun proxy based on the variant name + ScoringProfile.Weights + FunFactor
	/// </summary>
	public double Score(ValhallaRouteResponse candidate, RoutePlan plan, string variantName)
	{
		#region Guards

		if (candidate?.Trip is null) return double.NegativeInfinity;
		if (plan is null) return double.NegativeInfinity;

		#endregion

		#region Extract summary

		var km = candidate.Trip.Summary.Length;
		var routeMeters = km * 1000.0;

		#endregion

		#region Baseline distance (for detour ratio)

		// For point-to-point: baseline is great-circle distance start->end.
		// For loop: baseline is target distance (if present), otherwise use route length itself (ratio 1).
		double baselineMeters;

		if (plan.End is not null)
		{
			baselineMeters = HaversineMeters(plan.Start, plan.End);
			if (baselineMeters <= 1) baselineMeters = routeMeters; // safety
		}
		else if (plan.LoopSpec?.TargetDistance is not null)
		{
			baselineMeters = plan.LoopSpec.TargetDistance.Meters;
		}
		else
		{
			baselineMeters = routeMeters;
		}

		var detourRatio = routeMeters / baselineMeters;

		#endregion

		#region Constraint penalties

		// Hard penalty if violates max detour ratio.
		if (detourRatio > plan.Constraints.MaxDetourRatio)
			return -1_000_000;

		// Soft penalty if exceeds max distance.
		var distancePenalty = 0.0;
		if (plan.Constraints.MaxDistance is not null)
		{
			var cap = plan.Constraints.MaxDistance.Meters;
			if (routeMeters > cap)
			{
				var over = (routeMeters - cap) / cap; // relative overage
				distancePenalty = 250.0 * over;
			}
		}

		#endregion

		#region Fun proxy (V1)

		// Until we compute actual curvature/elevation/scenery metrics:
		// Assign a base “fun” from variant.
		var variantFun = variantName switch
		{
			"more-scenic" => 0.85,
			"balanced" => 0.60,
			"no-tolls" => 0.55,
			_ => 0.50
		};

		var weights = plan.ScoringProfile.Weights;

		// Weighted fun proxy
		var weightedFun =
			(weights.Curves * variantFun) +
			(weights.Scenery * variantFun) +
			(weights.Elevation * 0.25); // placeholder until elevation data exists

		// Apply FunFactor (0..10)
		var funScore = weightedFun * (1.0 + (plan.ScoringProfile.FunFactor / 10.0));

		#endregion

		#region Time bias (prefer shorter, but don’t over-optimize)

		// Use time as mild penalty so we don’t pick absurd detours.
		var timeSeconds = candidate.Trip.Summary.Time;
		var timePenalty = timeSeconds / 3600.0 * 0.25; // 0.25 point per hour

		#endregion

		#region Final score

		// Higher is better.
		return funScore - timePenalty - distancePenalty;

		#endregion
	}

	private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
	{
		const double R = 6371_000; // meters
		var dLat = ToRad(b.Latitude - a.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);

		var lat1 = ToRad(a.Latitude);
		var lat2 = ToRad(b.Latitude);

		var sinDLat = Math.Sin(dLat / 2);
		var sinDLon = Math.Sin(dLon / 2);

		var h = sinDLat * sinDLat + Math.Cos(lat1) * Math.Cos(lat2) * sinDLon * sinDLon;
		return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(h)));
	}

	private static double ToRad(double deg) => deg * Math.PI / 180.0;
}