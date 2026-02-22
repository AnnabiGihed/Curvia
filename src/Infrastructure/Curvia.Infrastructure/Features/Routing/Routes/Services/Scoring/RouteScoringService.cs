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
	/// <summary>
	/// Creates a new <see cref="RouteScoringService"/> instance.
	/// </summary>
	/// <param name="heightClient">Valhalla /height client used to fetch elevation samples along route geometry.</param>
	public RouteScoringService(IValhallaHeightClient heightClient)
	{
		_heightClient = heightClient ?? throw new ArgumentNullException(nameof(heightClient));
	}
	#endregion

	#region Private — Helpers
	/// <summary>
	/// Converts degrees to radians.
	/// </summary>
	private static double ToRad(double degrees)
	{
		return degrees * Math.PI / 180.0;
	}

	/// <summary>
	/// Computes Haversine distance in meters between two <see cref="GeoCoordinate"/> points.
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

	/// <summary>
	/// Computes the baseline distance used to evaluate detour ratio.
	/// - Point-to-point plans: baseline is the Haversine distance between Start and End (fallback to route length if tiny).
	/// - Loop plans: baseline is the target loop distance when provided.
	/// - Otherwise: baseline is the route length.
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
	#endregion

	#region Private — Elevation
	/// <summary>
	/// Computes an elevation-based score in the range [0, 1] by sampling heights along the route geometry.
	/// Falls back to 0.0 if the /height call fails or returns unusable data.
	/// </summary>
	private async Task<double> ComputeElevationScoreAsync(IReadOnlyList<GeoCoordinate> points, double routeMeters, CancellationToken cancellationToken)
	{
		try
		{
			var shapeLocations = points
				.Select(p => new ValhallaLocation(p.Latitude, p.Longitude, "through"))
				.ToList();

			var heightRequest = new ValhallaHeightRequest(Shape: shapeLocations, Range: false, ResampleDistance: null);

			var heightResponse = await _heightClient.GetHeightsAsync(heightRequest, cancellationToken);

			return ElevationAnalyzer.ComputeElevationScore(heightResponse.Height, routeMeters);
		}
		catch
		{
			return 0.0;
		}
	}
	#endregion

	#region IRouteScoringService
	/// <inheritdoc/>
	/// <summary>
	/// Scores a Valhalla candidate route for a given <see cref="RoutePlan"/> by combining:
	/// - curvature score (geometry-based)
	/// - elevation score (height sampling-based)
	/// - scenery proxy (lower average speed suggests more scenic roads)
	/// Then applies:
	/// - hard constraints (max duration, max detour ratio)
	/// - soft penalties (distance over cap, time beyond a threshold)
	/// Returns negative infinity for invalid inputs and a large negative number for hard constraint violations.
	/// </summary>
	public async Task<double> ScoreAsync(RouteCandidate candidate, RoutePlan plan, CancellationToken cancellationToken = default)
	{
		#region Guards
		if (candidate?.Raw?.Trip is null)
			return double.NegativeInfinity;

		if (plan is null)
			return double.NegativeInfinity;
		#endregion

		#region Extract Valhalla summary
		var trip = candidate.Raw.Trip;
		var timeSeconds = trip.Summary.Time;
		var routeMeters = trip.Summary.Length * 1000.0;
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
		var avgSpeedKph = timeSeconds > 0 ? (routeMeters / 1000.0) / (timeSeconds / 3600.0) : 130.0;
		var sceneryScore = Math.Max(0.0, Math.Min(1.0, 1.0 - avgSpeedKph / 130.0));
		#endregion

		#region Boredom penalty [0, 0.25]
		var straightFraction = 1.0 - curvatureScore;
		var boredomPenalty = straightFraction * 0.25;
		#endregion

		#region Fun score — weighted combination
		var weights = plan.ScoringProfile.Weights;

		var weightedCurvature = curvatureScore * weights.Curves;
		var weightedElevation = elevationScore * weights.Elevation;
		var weightedScenery = sceneryScore * weights.Scenery;

		var rawFunScore = weightedCurvature * 1.8 + weightedElevation * 1.2 + weightedScenery * 1.0 - boredomPenalty;

		var funScore = rawFunScore * (1.0 + plan.ScoringProfile.FunFactor / 10.0);
		#endregion

		#region Soft penalties
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
}