using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routes.Commands.Generate.Responses;
using Curvia.Application.Features.Routes.Contracts.Services.Scoring;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Builder;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routes.Commands.Generate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Orchestrates the full route generation pipeline.
///
///              Execution flow:
///                1.  Resolve scoring profile (preset or custom weights)
///                2.  Build RoutePlan domain aggregate from command
///                3.  Generate outbound candidates (3 Valhalla variants with lateral waypoints)
///                4.  Score candidates concurrently (Task.WhenAll)
///                5.  Pick the best outbound candidate
///                6.  Build outbound Route aggregate from Valhalla response
///                7a. [RoundTrip]      Generate End→Start return candidates
///                7b. [DifferentRoute] Generate Start→Start return candidates
///                8.  Map to response DTO
/// </summary>
internal sealed class GenerateRouteCommandHandler : ICommandHandler<GenerateRouteCommand, GenerateRouteResponse>
{
	#region Constants
	/// <summary>Valhalla graph version identifier embedded in every Route aggregate.</summary>
	private const string GraphVersion = "valhalla-europe-dem-v1";
	#endregion

	#region Fields
	private readonly IRouteBuilder _routeBuilder;
	private readonly IRouteScoringService _scoringService;
	private readonly IRouteCandidateGeneratorService _candidateGenerator;
	#endregion

	#region Constructor
	public GenerateRouteCommandHandler(
		IRouteBuilder routeBuilder,
		IRouteScoringService scoringService,
		IRouteCandidateGeneratorService candidateGeneratorService)
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

		#region 7. Return leg (RoundTrip or DifferentRoute loop)
		ReturnLegDto? returnLeg = null;

		if (command.RoundTrip && plan.End is not null)
		{
			var returnResult = await BuildRoundTripReturnLegAsync(plan, bestOutbound.Raw, cancellationToken);
			if (returnResult.IsFailure)
				return Result.Failure<GenerateRouteResponse>(returnResult.Error, returnResult.ResultExceptionType);
			returnLeg = returnResult.Value;
		}
		else if (plan.LoopSpec?.ReturnStrategy == ReturnStrategy.DifferentRoute)
		{
			var returnResult = await BuildLoopReturnLegAsync(plan, bestOutbound.Raw, cancellationToken);
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

	#region Private — Return leg builders

	private async Task<Result<ReturnLegDto>> BuildRoundTripReturnLegAsync(
		RoutePlan plan,
		ValhallaRouteResponse outboundResponse,
		CancellationToken cancellationToken)
	{
		IReadOnlyList<RouteCandidate> returnCandidates;

		try
		{
			returnCandidates = await _candidateGenerator.GenerateRoundTripReturnAsync(
				plan, outboundResponse, cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Failure<ReturnLegDto>(new Error(
				"Routing.Valhalla.RoundTripReturnFailed",
				$"Round-trip return leg generation failed: {ex.Message}"));
		}

		if (returnCandidates.Count == 0)
			return Result.Failure<ReturnLegDto>(new Error(
				"Routing.RoundTripReturn.NoCandidates",
				"No return leg candidates were generated for the round trip. " +
				"The outbound route corridor may be covering all available roads between these two points. " +
				"Try relaxing AvoidHighways or MaxDetourRatio."));

		return await ScoreAndBuildReturnLeg(plan, returnCandidates, cancellationToken);
	}

	private async Task<Result<ReturnLegDto>> BuildLoopReturnLegAsync(
		RoutePlan plan,
		ValhallaRouteResponse outboundResponse,
		CancellationToken cancellationToken)
	{
		IReadOnlyList<RouteCandidate> returnCandidates;

		try
		{
			returnCandidates = await _candidateGenerator.GenerateReturnAsync(
				plan, outboundResponse, cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Failure<ReturnLegDto>(new Error(
				"Routing.Valhalla.ReturnGenerationFailed",
				$"Return leg generation failed: {ex.Message}"));
		}

		if (returnCandidates.Count == 0)
			return Result.Failure<ReturnLegDto>(new Error(
				"Routing.ReturnCandidates.None",
				"No return leg candidates were generated."));

		return await ScoreAndBuildReturnLeg(plan, returnCandidates, cancellationToken);
	}

	private async Task<Result<ReturnLegDto>> ScoreAndBuildReturnLeg(
		RoutePlan plan,
		IReadOnlyList<RouteCandidate> candidates,
		CancellationToken cancellationToken)
	{
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
			return Result.Failure<ReturnLegDto>(new Error(
				"Routing.ReturnCandidates.AllDisqualified",
				"All return leg candidates were disqualified. Try relaxing your constraints."));

		var returnRouteResult = _routeBuilder.Build(plan, best.Raw, GraphVersion);
		if (returnRouteResult.IsFailure)
			return Result.Failure<ReturnLegDto>(returnRouteResult.Error, returnRouteResult.ResultExceptionType);

		var r = returnRouteResult.Value;

		return Result.Success(new ReturnLegDto
		{
			RouteId = r.Id.Value,
			FunScore = bestScore,
			FunRating = ComputeFunRating(bestScore),
			DistanceMeters = r.Stats.Distance.Meters,
			EstimatedDurationMinutes = (int)Math.Round((r.Stats.EstimatedDuration?.Seconds ?? 0) / 60.0),
			Polyline = r.Geometry.Points.Select(p => new[] { p.Latitude, p.Longitude }).ToList(),
			BoundingBox = new BoundingBoxDto(
				r.BoundingBox.MinLatitude, r.BoundingBox.MinLongitude,
				r.BoundingBox.MaxLatitude, r.BoundingBox.MaxLongitude)
		});
	}

	#endregion

	#region Private — Candidate selection

	private async Task<Result<(RouteCandidate Candidate, double Score)>> PickBestCandidateAsync(
		RoutePlan plan,
		CancellationToken cancellationToken)
	{
		IReadOnlyList<RouteCandidate> candidates;

		try
		{
			candidates = await _candidateGenerator.GenerateAsync(plan, cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Failure<(RouteCandidate, double)>(new Error(
				"Routing.Valhalla.GenerationFailed",
				$"Valhalla route generation failed: {ex.Message}"));
		}

		if (candidates.Count == 0)
			return Result.Failure<(RouteCandidate, double)>(new Error(
				"Routing.Candidates.None",
				"No route candidates were generated."));

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
			return Result.Failure<(RouteCandidate, double)>(new Error(
				"Routing.Candidates.AllDisqualified",
				"All route candidates were disqualified by the active constraints " +
				"(detour ratio, distance or duration limits). Try relaxing your constraints."));

		return Result.Success((best, bestScore));
	}

	#endregion

	#region Private — RoutePlan construction

	private static Result<ScoringProfile> BuildScoringProfile(GenerateRouteCommand cmd)
	{
		if (cmd.Preset != RoutePreset.Custom)
			return ScoringProfile.FromPreset(cmd.Preset);

		var weightsResult = ScoringWeights.Create(
			curves: cmd.CurvesWeight ?? 0,
			elevation: cmd.ElevationWeight ?? 0,
			scenery: cmd.SceneryWeight ?? 0);

		if (weightsResult.IsFailure)
			return Result.Failure<ScoringProfile>(weightsResult.Error, weightsResult.ResultExceptionType);

		return ScoringProfile.FromPreset(RoutePreset.Custom, cmd.FunFactor, weightsResult.Value);
	}

	private static Result<RoutePlan> BuildRoutePlan(GenerateRouteCommand cmd, ScoringProfile profile)
	{
		var startResult = GeoCoordinate.Create(cmd.StartLatitude, cmd.StartLongitude);
		if (startResult.IsFailure)
			return Result.Failure<RoutePlan>(startResult.Error, startResult.ResultExceptionType);

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

		Distance? maxDist = null;
		if (cmd.MaxDistanceMeters.HasValue)
		{
			var dr = Distance.Create(cmd.MaxDistanceMeters.Value);
			if (dr.IsFailure) return Result.Failure<RoutePlan>(dr.Error, dr.ResultExceptionType);
			maxDist = dr.Value;
		}

		long? maxDurationSec = cmd.MaxDurationMinutes.HasValue
			? (long)(cmd.MaxDurationMinutes.Value * 60)
			: null;

		var constraintsResult = RoutingConstraints.Create(
			maxDetourRatio: cmd.MaxDetourRatio,
			avoidHighways: cmd.AvoidHighways,
			avoidTolls: cmd.AvoidTolls,
			avoidUnpaved: cmd.AvoidUnpaved,
			urbanTolerance: cmd.UrbanTolerance,
			maxDistance: maxDist,
			maxDurationSeconds: maxDurationSec);

		if (constraintsResult.IsFailure)
			return Result.Failure<RoutePlan>(constraintsResult.Error, constraintsResult.ResultExceptionType);

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

	#region Private — Response mapping

	private static GenerateRouteResponse MapToResponse(
		Route route,
		string variantName,
		double score,
		ReturnLegDto? returnLeg)
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
			BoundingBox = new BoundingBoxDto(
				route.BoundingBox.MinLatitude, route.BoundingBox.MinLongitude,
				route.BoundingBox.MaxLatitude, route.BoundingBox.MaxLongitude)
		};
	}

	/// <summary>
	/// Converts a raw fun score to a 1–5 star rating calibrated to the Ardennes/Wallonia region.
	///
	/// CALIBRATION (updated alongside RouteGeometryAnalyzer and ElevationAnalyzer ceilings):
	///   The scoring ceilings are now benchmarked against the best roads in Belgium/Wallonia
	///   rather than Alpine passes. The achievable funScore range for the deployment region is
	///   approximately 0.0 (flat urban) to 3.0 (theoretical perfect route at all ceilings).
	///
	///   Thresholds are set so that:
	///     5★ (≥ 1.8) — Exceptional: dedicated twisty Ardennes loop on the very best roads
	///                   (Bouillon/Semois, Fonds de Quareux, Signal de Botrange circuit).
	///                   These routes require intentional planning for maximum curvature.
	///     4★ (≥ 0.9) — Excellent: good Ardennes secondary roads, Meuse gorge sections,
	///                   Durbuy S-curves. The kind of route a rider would specifically seek out.
	///     3★ (≥ 0.5) — Good: a mix of Ardennes hills and some flat sections. Worth riding,
	///                   noticeably more fun than urban commuting. Brussels→Houffalize falls here.
	///     2★ (≥ 0.15)— Average: mostly flat with occasional interest. Commuter-level route
	///                   that happens to use secondary roads.
	///     1★ (< 0.15) — Poor: flat, straight, or urban. Motorway-level boredom.
	///
	///   Previous thresholds (0.2 / 0.8 / 1.5 / 2.5) were calibrated for Alpine scores and
	///   caused the Brussels→Ardennes route (genuinely good riding) to rate 1★ permanently.
	/// </summary>
	private static int ComputeFunRating(double score) => score switch
	{
		>= 1.8 => 5,
		>= 0.9 => 4,   // was 1.0
		>= 0.5 => 3,
		>= 0.15 => 2,
		_ => 1
	};
	#endregion
}