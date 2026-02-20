using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Orchestrates the full route generation pipeline for a fun motorcycle ride.
///              Steps:
///              1. Resolve scoring profile from preset or custom weights.
///              2. Build and validate the <see cref="RoutePlan"/> domain aggregate.
///              3. Generate Valhalla route candidates (3 variants).
///              4. Score each candidate; apply hard duration cap and detour constraints.
///              5. Pick the highest-scoring candidate.
///              6. For DifferentRoute loops: repeat steps 3-5 with the outbound corridor excluded.
///              7. Build Route domain aggregate(s) from Valhalla response(s).
///              8. Map results to <see cref="GenerateRouteResponse"/> DTO.
/// </summary>
internal sealed class GenerateRouteCommandHandler : ICommandHandler<GenerateRouteCommand, GenerateRouteResponse>
{
	#region Fields
	/// <summary>
	/// Graph version pinned to V1 until the graph worker pipeline is active.
	/// Update this once graph versioning is implemented in the worker.
	/// </summary>
	private const string GraphVersion = "v1";

	private readonly IRouteBuilder _routeBuilder;
	private readonly IRouteScoringService _scoringService;
	private readonly IRouteCandidateGenerator _candidateGenerator;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises the handler with all required infrastructure services.
	/// </summary>
	/// <param name="candidateGenerator">Generates Valhalla route candidates.</param>
	/// <param name="scoringService">Scores candidates according to fun metrics and constraints.</param>
	/// <param name="routeBuilder">Builds the domain Route aggregate from a Valhalla response.</param>
	public GenerateRouteCommandHandler(IRouteCandidateGenerator candidateGenerator, IRouteScoringService scoringService, IRouteBuilder routeBuilder)
	{
		_routeBuilder = routeBuilder ?? throw new ArgumentNullException(nameof(routeBuilder));
		_scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
		_candidateGenerator = candidateGenerator ?? throw new ArgumentNullException(nameof(candidateGenerator));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Handles the <see cref="GenerateRouteCommand"/> and returns the best route found.
	/// </summary>
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
		var response = MapToResponse(outboundRoute, bestOutbound.VariantName, bestOutboundScore, returnLeg);
		return Result.Success(response);
		#endregion
	}
	#endregion

	#region Private — Response mapping
	/// <summary>
	/// Maps a built <see cref="Route"/> aggregate and metadata to the public response DTO.
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
	/// Converts a raw fun score to a 1–5 star rating for display in the UI.
	/// Thresholds are calibrated against the V1 scoring proxy; revisit when real metrics land.
	/// </summary>
	/// <param name="score">Raw score from <see cref="IRouteScoringService"/>.</param>
	/// <returns>Integer star rating from 1 (lowest) to 5 (highest).</returns>
	private static int ComputeFunRating(double score)
	{
		return score switch
		{
			>= 1.5 => 5,
			>= 1.0 => 4,
			>= 0.5 => 3,
			>= 0.0 => 2,
			_ => 1
		};
	}
	#endregion

	#region Private — Candidate selection
	/// <summary>
	/// Generates candidates, scores them and returns the best one along with its score.
	/// </summary>
	private async Task<Result<(RouteCandidate Candidate, double Score)>> PickBestCandidateAsync(RoutePlan plan, CancellationToken cancellationToken)
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

		RouteCandidate? best = null;
		double bestScore = double.NegativeInfinity;

		foreach (var candidate in candidates)
		{
			var score = _scoringService.Score(candidate.Raw, plan, candidate.VariantName);
			if (score > bestScore)
			{
				bestScore = score;
				best = candidate;
			}
		}

		// Disqualification threshold: all candidates were hard-penalised
		if (best is null || bestScore <= -999_999)
			return Result.Failure<(RouteCandidate, double)>(new Error("Routing.Candidates.AllDisqualified", "All route candidates were disqualified by the active constraints (detour ratio, distance or duration limits). Try relaxing your constraints."));

		return Result.Success((best, bestScore));
	}
	#endregion

	#region Private — RoutePlan construction
	/// <summary>
	/// Resolves the <see cref="ScoringProfile"/> from the command preset or custom weights.
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
	/// Builds the <see cref="RoutePlan"/> domain aggregate from the command fields.
	/// </summary>
	private static Result<RoutePlan> BuildRoutePlan(GenerateRouteCommand cmd, ScoringProfile profile)
	{
		#region Start coordinate
		var startResult = GeoCoordinate.Create(cmd.StartLatitude, cmd.StartLongitude);
		if (startResult.IsFailure)
			return Result.Failure<RoutePlan>(startResult.Error, startResult.ResultExceptionType);
		#endregion

		#region End coordinate (point-to-point) or LoopSpec
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
		#endregion

		#region Constraints
		Distance? maxDist = null;
		if (cmd.MaxDistanceMeters.HasValue)
		{
			var dr = Distance.Create(cmd.MaxDistanceMeters.Value);
			if (dr.IsFailure)
				return Result.Failure<RoutePlan>(dr.Error, dr.ResultExceptionType);
			maxDist = dr.Value;
		}

		long? maxDurationSec = cmd.MaxDurationMinutes.HasValue ? (long)(cmd.MaxDurationMinutes.Value * 60) : null;

		var constraintsResult = RoutingConstraints.Create(maxDetourRatio: cmd.MaxDetourRatio, avoidHighways: cmd.AvoidHighways, avoidTolls: cmd.AvoidTolls, avoidUnpaved: cmd.AvoidUnpaved, urbanTolerance: cmd.UrbanTolerance, maxDistance: maxDist, maxDurationSeconds: maxDurationSec);

		if (constraintsResult.IsFailure)
			return Result.Failure<RoutePlan>(constraintsResult.Error, constraintsResult.ResultExceptionType);
		#endregion

		#region Waypoints
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
		#endregion

		return RoutePlan.Create(startResult.Value, end, loopSpec, waypoints, constraintsResult.Value, profile);
	}
	#endregion

	#region Private — DifferentRoute return leg
	/// <summary>
	/// Generates the return leg for a <see cref="ReturnStrategy.DifferentRoute"/> loop.
	/// Passes the outbound route response to the generator so it can build an exclusion zone.
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

		RouteCandidate? bestReturn = null;
		double bestReturnScore = double.NegativeInfinity;

		foreach (var c in returnCandidates)
		{
			var s = _scoringService.Score(c.Raw, plan, c.VariantName);
			if (s > bestReturnScore)
			{
				bestReturnScore = s;
				bestReturn = c;
			}
		}

		if (bestReturn is null || bestReturnScore <= -999_999)
			return Result.Failure<ReturnLegDto>(new Error("Routing.ReturnCandidates.AllDisqualified", "All return leg candidates were disqualified. The return leg will be omitted."));

		var returnRouteResult = _routeBuilder.Build(plan, bestReturn.Raw, GraphVersion);
		if (returnRouteResult.IsFailure)
			return Result.Failure<ReturnLegDto>(returnRouteResult.Error, returnRouteResult.ResultExceptionType);

		var returnRoute = returnRouteResult.Value;

		return Result.Success(new ReturnLegDto
		{
			FunScore = bestReturnScore,
			RouteId = returnRoute.Id.Value,
			FunRating = ComputeFunRating(bestReturnScore),
			DistanceMeters = returnRoute.Stats.Distance.Meters,
			EstimatedDurationMinutes = (int)Math.Round((returnRoute.Stats.EstimatedDuration?.Seconds ?? 0) / 60.0),
			Polyline = returnRoute.Geometry.Points.Select(p => new[] { p.Latitude, p.Longitude }).ToList(),
			BoundingBox = new BoundingBoxDto(returnRoute.BoundingBox.MinLatitude, returnRoute.BoundingBox.MinLongitude, returnRoute.BoundingBox.MaxLatitude, returnRoute.BoundingBox.MaxLongitude)
		});
	}
	#endregion
}