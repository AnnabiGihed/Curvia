using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Infrastructure.Features.Routing.Routes.Helpers;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Application.Features.Routing.Routes.Contracts.Services.Scoring;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.HeightClient;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

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
///                7. Soft penalties (distance overage, time bias)
///
///              Final formula (design doc):
///                FunScore = CurvatureScore × 1.8 + ElevationScore × 1.2 + SceneryScore × 1.0
///                         − BoredomPenalty × 2.0
///                Cost     = BaseDistance − (FunScore × FunFactor)
/// </summary>
internal sealed class RouteScoringService : IRouteScoringService
{
	#region Fields
	private readonly IValhallaHeightClient _heightClient;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises the service with the Valhalla height client for elevation queries.
	/// </summary>
	/// <param name="heightClient">Client for Valhalla's /height endpoint.</param>
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
		if (plan.Constraints.MaxDurationSeconds.HasValue &&	timeSeconds > plan.Constraints.MaxDurationSeconds.Value)
			return -1_000_000;
		#endregion

		#region Hard constraint — MaxDetourRatio
		var baselineMeters = ComputeBaselineMeters(plan, routeMeters);
		var detourRatio = routeMeters / baselineMeters;

		if (detourRatio > plan.Constraints.MaxDetourRatio)
			return -1_000_000;
		#endregion

		#region Decode route geometry
		// Collect all shape points from all legs
		var allPoints = trip.Legs
			.Where(l => !string.IsNullOrWhiteSpace(l.Shape))
			.SelectMany(l => Polyline6Decoder.Decode(l.Shape))
			.ToList();

		if (allPoints.Count < 2)
			return -1_000_000;
		#endregion

		#region Real curvature score
		var curvatureScore = RouteGeometryAnalyzer.ComputeCurvatureScore(allPoints);
		#endregion

		#region Real elevation score (Valhalla /height)
		var elevationScore = await ComputeElevationScoreAsync(allPoints, routeMeters, cancellationToken);
		#endregion

		#region Scenery score — average road speed proxy
		// Slower average speed → rural/mountain roads → more scenic.
		// 130 km/h = pure motorway (score 0), 30 km/h = mountain village (score ~0.77).
		// Formula: SceneryScore = 1 - (avgSpeedKph / 130) clamped to [0, 1]
		var avgSpeedKph = timeSeconds > 0 ? (routeMeters / 1000.0) / (timeSeconds / 3600.0) : 130.0;

		var sceneryScore = Math.Max(0.0, Math.Min(1.0, 1.0 - avgSpeedKph / 130.0));
		#endregion

		#region Boredom penalty — straight road fraction
		// Estimate straight-road fraction from curvature:
		// Low curvature = high boredom. Penalty increases for monotonous routes.
		// BoredomPenalty ∈ [0, 1] — inverse of curvature score weighted by distance bias.
		var straightFraction = 1.0 - curvatureScore;
		var boredomPenalty = straightFraction * 0.5; // max 0.5 penalty, not a hard disqualifier
		#endregion

		#region Fun score (design doc formula, user-weight adjusted)
		var weights = plan.ScoringProfile.Weights;

		// Apply user weights to the three raw scores
		var userWeightedCurvature = curvatureScore * weights.Curves;
		var userWeightedElevation = elevationScore * weights.Elevation;
		var userWeightedScenery = sceneryScore * weights.Scenery;

		// Apply design doc multipliers (curvature matters most for motorcycles)
		var rawFunScore =userWeightedCurvature * 1.8 + userWeightedElevation * 1.2 + userWeightedScenery * 1.0 - boredomPenalty * 2.0;

		// Apply global FunFactor multiplier (range [0,10] → multiplier [1.0, 2.0])
		var funScore = rawFunScore * (1.0 + plan.ScoringProfile.FunFactor / 10.0);
		#endregion

		#region Soft penalties
		// Distance overage penalty
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

		// Time bias: mildly prefer shorter rides when fun scores are equal
		// (0.25 pts per hour — intentionally small to not override fun)
		var timePenalty = timeSeconds / 3600.0 * 0.25;
		#endregion

		#region Final score
		return funScore - timePenalty - distancePenalty;
		#endregion
	}
	#endregion

	#region Private — Elevation
	/// <summary>
	/// Queries Valhalla /height for elevation data and computes the elevation score.
	/// Falls back to 0.0 if the height service is unavailable, so a single endpoint failure
	/// does not disqualify an otherwise excellent route.
	/// </summary>
	private async Task<double> ComputeElevationScoreAsync(IReadOnlyList<GeoCoordinate> points, double routeMeters, CancellationToken cancellationToken)
	{
		try
		{
			var shapeLocations = points
				.Select(p => new ValhallaLocation(p.Latitude, p.Longitude, "through"))
				.ToList();

			var heightRequest = new ValhallaHeightRequest(	Shape: shapeLocations, Range: false, ResampleDistance: null);

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
	/// Computes the great-circle distance in meters between two coordinates (Haversine formula).
	/// </summary>
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