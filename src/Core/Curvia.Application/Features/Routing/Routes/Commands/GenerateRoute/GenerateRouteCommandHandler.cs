using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routing.Routes.Contracts.Services.Scoring;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Builder;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Orchestrates the full fun motorcycle route generation pipeline.
///              Steps:
///              1. Resolve scoring profile (preset or custom weights).
///              2. Build and validate the RoutePlan domain aggregate.
///              3. Generate Valhalla route candidates (3 variants per leg).
///              4. Score each candidate using real curvature, elevation and scenery analysis.
///              5. Pick the highest-scoring compliant candidate.
///              6. For DifferentRoute loops: repeat steps 3–5 for the return leg with exclusion zone.
///              7. Build Route domain aggregate(s) and map to response DTO.
/// </summary>
internal sealed class GenerateRouteCommandHandler : ICommandHandler<GenerateRouteCommand, GenerateRouteResponse>
{
	#region Fields
	/// <summary>Graph version pinned to v1 until the graph worker pipeline is operational.</summary>
	private const string GraphVersion = "v1";

	private readonly IRouteBuilder _routeBuilder;
	private readonly IRouteScoringService _scoringService;
	private readonly IRouteCandidateGeneratorService _candidateGenerator;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises the handler with all required infrastructure services.
	/// </summary>
	/// <param name="candidateGeneratorService">Generates Valhalla route candidates.</param>
	/// <param name="scoringService">Scores candidates using real geometric analysis.</param>
	/// <param name="routeBuilder">Builds the Route domain aggregate from a Valhalla response.</param>
	public GenerateRouteCommandHandler(IRouteCandidateGeneratorService candidateGeneratorService, IRouteScoringService scoringService, IRouteBuilder routeBuilder)
	{
		_routeBuilder = routeBuilder ?? throw new ArgumentNullException(nameof(routeBuilder));
		_scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
		_candidateGenerator = candidateGeneratorService ?? throw new ArgumentNullException(nameof(candidateGeneratorService));
	}
	#endregion

	#region ICommandHandler
	/// <inheritdoc/>
	public async Task<Result<GenerateRouteResponse>> Handle(GenerateRouteCommand command, CancellationToken cancellationToken)
	{
		#region 1. Resolve scoring profile
		var profileResult = BuildScoringProfile(command);
		if (profileResult.IsFailure)
			return Result.Failure<GenerateRouteResponse>(profileResult.Error, profileResult.ResultExceptionType);
		#endregion

		#region 2. Build RoutePlan aggregate
		var planResult = BuildRoutePlan(command, profileResult.Value);
		if (planResult.IsFailure)
			return Result.Failure<GenerateRouteResponse>(planResult.Error, planResult.ResultExceptionType);

		var plan = planResult.Value;
		#endregion

		#region 3–5. Generate, score and pick best outbound candidate
		var outboundResult = await PickBestCandidateAsync(plan, cancellationToken);
		if (outboundResult.IsFailure)
			return Result.Failure<GenerateRouteResponse>(outboundResult.Error, outboundResult.ResultExceptionType);

		var (bestOutbound, bestOutboundScore) = outboundResult.Value;
		#endregion

		#region 6. Build outbound Route aggregate
		var outboundRouteResult = _routeBuilder.Build(plan, bestOutbound.Raw, GraphVersion);
		if (outboundRouteResult.IsFailure)
			return Result.Failure<GenerateRouteResponse>(outboundRouteResult.Error, outboundRouteResult.ResultExceptionType);

		var outboundRoute = outboundRouteResult.Value;
		#endregion

		#region 7. DifferentRoute loop — generate return leg
		ReturnLegDto? returnLeg = null;

		if (plan.LoopSpec?.ReturnStrategy == ReturnStrategy.DifferentRoute)
		{
			var returnResult = await BuildReturnLegAsync(plan, bestOutbound.Raw, cancellationToken);
			if (returnResult.IsFailure)
				return Result.Failure<GenerateRouteResponse>(returnResult.Error, returnResult.ResultExceptionType);
			returnLeg = returnResult.Value;
		}
		#endregion

		#region 8. Map to response DTO
		return Result.Success(MapToResponse(outboundRoute, bestOutbound.VariantName, bestOutboundScore, returnLeg));
		#endregion
	}
	#endregion

	#region Private — Response mapping
	/// <summary>
	/// Maps the built Route aggregate and metadata to the public response DTO.
	/// </summary>
	private static GenerateRouteResponse MapToResponse(Route route, string variantName, double score, ReturnLegDto? returnLeg)
	{
		return new GenerateRouteResponse
		{
			FunScore = score,
			ReturnLeg = returnLeg,
			RouteId = route.Id.Value,
			VariantName = variantName,
			FunRating = ComputeFunRating(score),
			DistanceMeters = route.Stats.Distance.Meters,
			EstimatedDurationMinutes = (int)Math.Round((route.Stats.EstimatedDuration?.Seconds ?? 0) / 60.0),
			Polyline = route.Geometry.Points.Select(p => new[] { p.Latitude, p.Longitude }).ToList(),
			BoundingBox = new BoundingBoxDto(route.BoundingBox.MinLatitude, route.BoundingBox.MinLongitude, route.BoundingBox.MaxLatitude, route.BoundingBox.MaxLongitude)
		};
	}

	/// <summary>
	/// Converts a raw fun score to a 1–5 star rating for display in the mobile UI.
	/// Thresholds are calibrated against the real metric ranges:
	///   Score &gt;= 2.5 → 5 stars (Alpine-class twisty mountain roads)
	///   Score &gt;= 1.5 → 4 stars (Excellent rural/mountain riding)
	///   Score &gt;= 0.8 → 3 stars (Good secondary roads, some curves)
	///   Score &gt;= 0.2 → 2 stars (Mostly boring, some mild interest)
	///   Score  &lt; 0.2 → 1 star  (Flat/straight/urban route)
	/// </summary>
	private static int ComputeFunRating(double score)
	{
		return score switch
		{
			>= 2.5 => 5,
			>= 1.5 => 4,
			>= 0.8 => 3,
			>= 0.2 => 2,
			_ => 1
		};
	}
	#endregion

	#region Private — Candidate selection
	/// <summary>
	/// Generates candidates, scores them concurrently and returns the best one with its score.
	/// Scoring runs concurrently across all candidates to minimise total latency.
	/// </summary>
	private async Task<Result<(RouteCandidate Candidate, double Score)>> PickBestCandidateAsync(RoutePlan plan,	CancellationToken cancellationToken)
	{
		IReadOnlyList<RouteCandidate> candidates;

		try
		{
			candidates = await _candidateGenerator.GenerateAsync(plan, cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Failure<(RouteCandidate, double)>(new Error("Routing.Valhalla.GenerationFailed", $"Valhalla route generation failed: {ex.Message}"));
		}

		if (candidates.Count == 0)
			return Result.Failure<(RouteCandidate, double)>(new Error("Routing.Candidates.None", "No route candidates were generated."));

		// Score all candidates concurrently for minimal latency
		var scoreTasks = candidates
			.Select(c => _scoringService.ScoreAsync(c, plan, cancellationToken))
			.ToList();

		var scores = await Task.WhenAll(scoreTasks);

		RouteCandidate? best = null;
		double bestScore = double.NegativeInfinity;

		for (var i = 0; i < candidates.Count; i++)
		{
			if (scores[i] > bestScore)
			{
				bestScore = scores[i];
				best = candidates[i];
			}
		}

		if (best is null || bestScore <= -999_999)
			return Result.Failure<(RouteCandidate, double)>(new Error("Routing.Candidates.AllDisqualified", "All route candidates were disqualified by the active constraints " + "(detour ratio, distance or duration limits). Try relaxing your constraints."));

		return Result.Success((best, bestScore));
	}

	#endregion

	#region Private — RoutePlan construction
	/// <summary>
	/// Resolves the scoring profile from the command preset or custom weights.
	/// </summary>
	private static Result<ScoringProfile> BuildScoringProfile(GenerateRouteCommand cmd)
	{
		if (cmd.Preset != RoutePreset.Custom)
			return ScoringProfile.FromPreset(cmd.Preset);

		var weightsResult = ScoringWeights.Create(curves: cmd.CurvesWeight ?? 0, elevation: cmd.ElevationWeight ?? 0, scenery: cmd.SceneryWeight ?? 0);

		if (weightsResult.IsFailure)
			return Result.Failure<ScoringProfile>(weightsResult.Error, weightsResult.ResultExceptionType);

		return ScoringProfile.FromPreset(RoutePreset.Custom, cmd.FunFactor, weightsResult.Value);
	}

	/// <summary>
	/// Builds the RoutePlan domain aggregate from the command fields.
	/// </summary>
	private static Result<RoutePlan> BuildRoutePlan(GenerateRouteCommand cmd, ScoringProfile profile)
	{
		// Start coordinate
		var startResult = GeoCoordinate.Create(cmd.StartLatitude, cmd.StartLongitude);
		if (startResult.IsFailure)
			return Result.Failure<RoutePlan>(startResult.Error, startResult.ResultExceptionType);

		// End coordinate (point-to-point) or LoopSpec
		GeoCoordinate? end = null;
		LoopSpec? loopSpec = null;

		if (cmd.EndLatitude.HasValue && cmd.EndLongitude.HasValue)
		{
			var endResult = GeoCoordinate.Create(cmd.EndLatitude.Value, cmd.EndLongitude.Value);
			if (endResult.IsFailure)
				return Result.Failure<RoutePlan>(endResult.Error, endResult.ResultExceptionType);

			end = endResult.Value;
		}
		else
		{
			var distResult = Distance.Create(cmd.LoopTargetDistanceMeters!.Value);
			if (distResult.IsFailure)
				return Result.Failure<RoutePlan>(distResult.Error, distResult.ResultExceptionType);

			var loopResult = LoopSpec.Create(distResult.Value, cmd.LoopReturnStrategy);
			if (loopResult.IsFailure)
				return Result.Failure<RoutePlan>(loopResult.Error, loopResult.ResultExceptionType);

			loopSpec = loopResult.Value;
		}

		// MaxDistance constraint
		Distance? maxDist = null;
		if (cmd.MaxDistanceMeters.HasValue)
		{
			var dr = Distance.Create(cmd.MaxDistanceMeters.Value);
			if (dr.IsFailure)
				return Result.Failure<RoutePlan>(dr.Error, dr.ResultExceptionType);

			maxDist = dr.Value;
		}

		// MaxDuration constraint (minutes → seconds)
		long? maxDurationSec = cmd.MaxDurationMinutes.HasValue ? (long)(cmd.MaxDurationMinutes.Value * 60) : null;

		var constraintsResult = RoutingConstraints.Create(maxDetourRatio: cmd.MaxDetourRatio, avoidHighways: cmd.AvoidHighways, avoidTolls: cmd.AvoidTolls, avoidUnpaved: cmd.AvoidUnpaved, urbanTolerance: cmd.UrbanTolerance, maxDistance: maxDist, maxDurationSeconds: maxDurationSec);

		if (constraintsResult.IsFailure)
			return Result.Failure<RoutePlan>(constraintsResult.Error, constraintsResult.ResultExceptionType);

		// Waypoints
		List<Waypoint>? waypoints = null;
		if (cmd.Waypoints is { Count: > 0 })
		{
			waypoints = new List<Waypoint>(cmd.Waypoints.Count);
			foreach (var wp in cmd.Waypoints)
			{
				var coordResult = GeoCoordinate.Create(wp.Latitude, wp.Longitude);
				if (coordResult.IsFailure)
					return Result.Failure<RoutePlan>(coordResult.Error, coordResult.ResultExceptionType);

				var waypointResult = Waypoint.Create(coordResult.Value);
				if (waypointResult.IsFailure)
					return Result.Failure<RoutePlan>(waypointResult.Error, waypointResult.ResultExceptionType);

				waypoints.Add(waypointResult.Value);
			}
		}

		return RoutePlan.Create(startResult.Value, end, loopSpec, waypoints, constraintsResult.Value, profile);
	}
	#endregion

	#region Private — DifferentRoute return leg
	/// <summary>
	/// Generates and scores the return leg for a DifferentRoute loop,
	/// then builds the return Route aggregate and maps it to a ReturnLegDto.
	/// </summary>
	private async Task<Result<ReturnLegDto>> BuildReturnLegAsync(RoutePlan plan, ValhallaRouteResponse outboundResponse, CancellationToken cancellationToken)
	{
		IReadOnlyList<RouteCandidate> returnCandidates;

		try
		{
			returnCandidates = await _candidateGenerator.GenerateReturnAsync(plan, outboundResponse, cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Failure<ReturnLegDto>(new Error("Routing.Valhalla.ReturnGenerationFailed", $"Return leg generation failed: {ex.Message}"));
		}

		if (returnCandidates.Count == 0)
			return Result.Failure<ReturnLegDto>(new Error("Routing.ReturnCandidates.None", "No return leg candidates were generated."));

		// Score concurrently
		var scoreTasks = returnCandidates
			.Select(c => _scoringService.ScoreAsync(c, plan, cancellationToken))
			.ToList();

		var scores = await Task.WhenAll(scoreTasks);

		RouteCandidate? bestReturn = null;
		double bestReturnScore = double.NegativeInfinity;

		for (var i = 0; i < returnCandidates.Count; i++)
		{
			if (scores[i] > bestReturnScore)
			{
				bestReturnScore = scores[i];
				bestReturn = returnCandidates[i];
			}
		}

		if (bestReturn is null || bestReturnScore <= -999_999)
			return Result.Failure<ReturnLegDto>(new Error("Routing.ReturnCandidates.AllDisqualified", "All return leg candidates were disqualified. Try relaxing your constraints."));

		var returnRouteResult = _routeBuilder.Build(plan, bestReturn.Raw, GraphVersion);
		if (returnRouteResult.IsFailure)
			return Result.Failure<ReturnLegDto>(returnRouteResult.Error, returnRouteResult.ResultExceptionType);

		var r = returnRouteResult.Value;

		return Result.Success(new ReturnLegDto
		{
			RouteId = r.Id.Value,
			FunScore = bestReturnScore,
			DistanceMeters = r.Stats.Distance.Meters,
			FunRating = ComputeFunRating(bestReturnScore),
			EstimatedDurationMinutes = (int)Math.Round((r.Stats.EstimatedDuration?.Seconds ?? 0) / 60.0),
			Polyline = r.Geometry.Points.Select(p => new[] { p.Latitude, p.Longitude }).ToList(),
			BoundingBox = new BoundingBoxDto(r.BoundingBox.MinLatitude, r.BoundingBox.MinLongitude, r.BoundingBox.MaxLatitude, r.BoundingBox.MaxLongitude)
		});
	}
	#endregion
}