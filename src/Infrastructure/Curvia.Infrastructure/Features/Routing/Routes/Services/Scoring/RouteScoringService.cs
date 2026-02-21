using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Infrastructure.Features.Routing.Routes.Helpers;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Application.Features.Routes.Contracts.Services.Scoring;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Models;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.HeightClient;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services.Scoring;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Scores Valhalla route candidates using real geometric and elevation analysis.
///
///              Scoring pipeline per candidate:
///                1. Hard constraint guards (detour ratio, duration cap) → disqualify if violated
///                2. Real curvature score from decoded polyline (RouteGeometryAnalyzer)
///                3. Real elevation score from Valhalla /height endpoint (ElevationAnalyzer)
///                4. Scenery proxy from average road speed (slower = more rural = more scenic)
///                5. Weighted combination using the plan's ScoringProfile
///                6. Apply FunFactor multiplier
///                7. Soft penalties (distance overage, ultra-long ride penalty)
///
///              Final formula (corrected from design doc):
///                FunScore = CurvatureScore × 1.8 + ElevationScore × 1.2 + SceneryScore × 1.0
///                         − BoredomPenalty
///
///              Design doc formula fix:
///                The original design doc specifies BoredomPenalty × 2.0.
///                Combined with boredomPenalty = straightFraction × 0.5, this expands to:
///                  − (1 - curvatureScore) × 1.0
///                  = − 1.0 + curvatureScore × 1.0
///                This introduces a structural constant −1.0 offset that causes all routes
///                with moderate curvature to score negative before any other contribution.
///                Fixed: boredomPenalty = straightFraction × 0.25 (max penalty 0.25, not 1.0).
///                This preserves the design intent (penalise boring routes) without making
///                any route negative by structural default.
///
///              Time penalty fix:
///                The original 0.25 pts/hour penalty deducted 0.65 pts on a 157-minute Ardennes
///                ride, pushing scores deeply negative for long enjoyable routes.
///                Fixed: only penalise rides that exceed 6 hours (ultra-long inefficiency).
///                Normal rides (under 6 hours) receive zero time penalty.
/// </summary>
internal sealed class RouteScoringService : IRouteScoringService
{
	#region Fields
	private readonly IValhallaHeightClient _heightClient;

	/// <summary>
	/// Rides under this threshold receive zero time penalty.
	/// Penalising normal 2–4 hour rides was incorrectly discouraging long enjoyable routes.
	/// </summary>
	private const double TimePenaltyThresholdSeconds = 6 * 3600; // 6 hours

	/// <summary>
	/// Penalty rate per hour applied only beyond the threshold.
	/// 0.1 pts/hour means an 8-hour ride costs 0.2 extra pts — a soft discouragement only.
	/// </summary>
	private const double TimePenaltyRatePerHour = 0.1;
	#endregion

	#region Constructor
	public RouteScoringService(IValhallaHeightClient heightClient)
	{
		_heightClient = heightClient ?? throw new ArgumentNullException(nameof(heightClient));
	}
	#endregion

	#region IRouteScoringService
	/// <inheritdoc/>
	public async Task<double> ScoreAsync(RouteCandidate candidate, RoutePlan plan, CancellationToken cancellationToken = default)
	{
		#region Guards
		if (candidate?.Raw?.Trip is null) return double.NegativeInfinity;
		if (plan is null) return double.NegativeInfinity;
		#endregion

		#region Extract Valhalla summary
		var trip = candidate.Raw.Trip;
		var routeMeters = trip.Summary.Length * 1000.0;
		var timeSeconds = trip.Summary.Time;
		#endregion

		#region Hard constraint — MaxDurationSeconds
		if (plan.Constraints.MaxDurationSeconds.HasValue &&
			timeSeconds > plan.Constraints.MaxDurationSeconds.Value)
			return -1_000_000;
		#endregion

		#region Hard constraint — MaxDetourRatio
		var baselineMeters = ComputeBaselineMeters(plan, routeMeters);
		var detourRatio = routeMeters / baselineMeters;

		if (detourRatio > plan.Constraints.MaxDetourRatio)
			return -1_000_000;
		#endregion

		#region Decode route geometry
		var allPoints = trip.Legs
			.Where(l => !string.IsNullOrWhiteSpace(l.Shape))
			.SelectMany(l => Polyline6Decoder.Decode(l.Shape))
			.ToList();

		if (allPoints.Count < 2)
			return -1_000_000;
		#endregion

		#region Real curvature score [0, 1]
		var curvatureScore = RouteGeometryAnalyzer.ComputeCurvatureScore(allPoints);
		#endregion

		#region Real elevation score [0, 1] — Valhalla /height
		var elevationScore = await ComputeElevationScoreAsync(allPoints, routeMeters, cancellationToken);
		#endregion

		#region Scenery score — average road speed proxy [0, 1]
		// Slower average speed → rural / mountain roads → more scenic.
		// 130 km/h = motorway (score 0.0), 30 km/h = mountain village (score 0.77).
		// Formula: SceneryScore = 1 - (avgSpeedKph / 130) clamped to [0, 1]
		var avgSpeedKph = timeSeconds > 0
			? (routeMeters / 1000.0) / (timeSeconds / 3600.0)
			: 130.0;
		var sceneryScore = Math.Max(0.0, Math.Min(1.0, 1.0 - avgSpeedKph / 130.0));
		#endregion

		#region Boredom penalty [0, 0.25]
		// Penalises straight, monotonous routes.
		// Max penalty is 0.25 (on a perfectly straight route with zero curvature).
		//
		// Design doc bug: original formula was boredomPenalty × 2.0 with
		// boredomPenalty = straightFraction × 0.5, which expands to:
		//   − (1 − curvatureScore) × 1.0 = − 1.0 + curvatureScore
		// introducing a structural constant −1.0 that makes nearly all routes score negative.
		//
		// Fixed: boredomPenalty = straightFraction × 0.25
		// Applied directly in the formula (× 1.0, not × 2.0).
		var straightFraction = 1.0 - curvatureScore;
		var boredomPenalty = straightFraction * 0.25;
		#endregion

		#region Fun score — weighted combination
		var weights = plan.ScoringProfile.Weights;

		var weightedCurvature = curvatureScore * weights.Curves;
		var weightedElevation = elevationScore * weights.Elevation;
		var weightedScenery = sceneryScore * weights.Scenery;

		// Design doc multipliers (curvature matters most for motorcycles)
		var rawFunScore = weightedCurvature * 1.8
						+ weightedElevation * 1.2
						+ weightedScenery * 1.0
						- boredomPenalty;

		// FunFactor multiplier: range [0, 10] → multiplier [1.0, 2.0]
		var funScore = rawFunScore * (1.0 + plan.ScoringProfile.FunFactor / 10.0);
		#endregion

		#region Soft penalties

		// Distance overage — penalise routes exceeding the hard distance cap
		var distancePenalty = 0.0;
		if (plan.Constraints.MaxDistance is not null)
		{
			var cap = plan.Constraints.MaxDistance.Meters;
			if (routeMeters > cap)
			{
				var overage = (routeMeters - cap) / cap;
				distancePenalty = 250.0 * overage;
			}
		}

		// Time penalty — only applied to rides exceeding 6 hours.
		// Original 0.25 pts/hour rate deducted 0.65 pts on a 157-min ride, incorrectly
		// pushing enjoyable long routes into deeply negative territory.
		// Fixed: threshold at 6 hours, rate reduced to 0.1 pts/hour beyond the threshold.
		var timePenalty = 0.0;
		if (timeSeconds > TimePenaltyThresholdSeconds)
		{
			var excessHours = (timeSeconds - TimePenaltyThresholdSeconds) / 3600.0;
			timePenalty = excessHours * TimePenaltyRatePerHour;
		}

		#endregion

		#region Final score
		return funScore - timePenalty - distancePenalty;
		#endregion
	}
	#endregion

	#region Private — Elevation
	private async Task<double> ComputeElevationScoreAsync(
		IReadOnlyList<GeoCoordinate> points,
		double routeMeters,
		CancellationToken cancellationToken)
	{
		try
		{
			var shapeLocations = points
				.Select(p => new ValhallaLocation(p.Latitude, p.Longitude, "through"))
				.ToList();

			var heightRequest = new ValhallaHeightRequest(
				Shape: shapeLocations,
				Range: false,
				ResampleDistance: null);

			var heightResponse = await _heightClient.GetHeightsAsync(heightRequest, cancellationToken);

			return ElevationAnalyzer.ComputeElevationScore(heightResponse.Height, routeMeters);
		}
		catch
		{
			// Elevation data is a scoring enhancement, not a hard requirement.
			// If /height is unavailable, degrade gracefully to 0 elevation contribution.
			return 0.0;
		}
	}
	#endregion

	#region Private — Helpers
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

	private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
	#endregion
}