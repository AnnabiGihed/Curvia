using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.Enums;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routes.Contracts.Services.Scoring;
using Curvia.Application.Features.Routes.Commands.Generate.Responses;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Builder;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routes.Commands.Generate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Orchestrates the full fun-route generation pipeline.
///              Resolves the scoring profile, builds the RoutePlan aggregate, generates
///              Valhalla candidates, scores them, picks the best, builds the Route aggregate,
///              and optionally generates a return leg for round-trip or DifferentRoute loop modes.
/// </summary>
internal sealed class GenerateRouteCommandHandler : ICommandHandler<GenerateRouteCommand, GenerateRouteResponse>
{
	#region Constants
	/// <summary>Valhalla graph version identifier embedded in every generated Route aggregate for reproducibility.</summary>
	private const string GraphVersion = "valhalla-europe-dem-v1";
	#endregion

	#region Fields
	private readonly IRouteBuilder _routeBuilder;
	private readonly IRouteScoringService _scoringService;
	private readonly IRouteCandidateGeneratorService _candidateGenerator;
	#endregion

	#region Constructor
	/// <summary>
	/// Creates a new <see cref="GenerateRouteCommandHandler"/> instance.
	/// </summary>
	/// <param name="routeBuilder">Builds the Route domain aggregate from Valhalla responses.</param>
	/// <param name="scoringService">Scores route candidates against the plan's constraints and profile.</param>
	/// <param name="candidateGeneratorService">Generates Valhalla route candidates for a given plan.</param>
	public GenerateRouteCommandHandler(IRouteBuilder routeBuilder, IRouteScoringService scoringService, IRouteCandidateGeneratorService candidateGeneratorService)
	{
		_routeBuilder = routeBuilder ?? throw new ArgumentNullException(nameof(routeBuilder));
		_scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
		_candidateGenerator = candidateGeneratorService ?? throw new ArgumentNullException(nameof(candidateGeneratorService));
	}
	#endregion

	#region ICommandHandler
	/// <inheritdoc/>
	public async Task<Result<GenerateRouteResponse>> Handle(GenerateRouteCommand command, CancellationToken ct)
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
		var outboundResult = await PickBestCandidateAsync(plan, ct);
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
			var returnResult = await BuildReturnLegAsync(plan, bestOutbound.Raw, ct);
			if (returnResult.IsFailure)
				return Result.Failure<GenerateRouteResponse>(returnResult.Error, returnResult.ResultExceptionType);

			returnLeg = returnResult.Value;
		}
		else if (plan.LoopSpec?.ReturnStrategy == ReturnStrategy.DifferentRoute)
		{
			var returnResult = await BuildLoopReturnLegAsync(plan, bestOutbound.Raw, ct);
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

	#region Private — Candidate selection
	/// <summary>
	/// Generates candidates for the outbound leg, scores them, and returns the best scoring one.
	/// All candidates are scored concurrently. A candidate with score ≤ -999,999 is considered
	/// disqualified by a hard constraint.
	/// </summary>
	private async Task<Result<(RouteCandidate, double)>> PickBestCandidateAsync(RoutePlan plan, CancellationToken ct)
	{
		var candidates = await _candidateGenerator.GenerateAsync(plan, ct);

		if (candidates.Count == 0)
			return Result.Failure<(RouteCandidate, double)>(new Error("Routing.Candidates.None", "No route candidates were returned by Valhalla. Check that the start and end coordinates are reachable."));

		var scores = await Task.WhenAll(candidates.Select(c => _scoringService.ScoreAsync(c, plan, ct)));

		RouteCandidate? best = null;
		var bestScore = double.NegativeInfinity;

		for (var i = 0; i < candidates.Count; i++)
		{
			if (scores[i] > bestScore)
			{
				bestScore = scores[i];
				best = candidates[i];
			}
		}

		if (best is null || bestScore <= -999_999)
			return Result.Failure<(RouteCandidate, double)>(new Error("Routing.Candidates.AllDisqualified", "All route candidates were disqualified by the active constraints (detour ratio, distance or duration limits). Try relaxing your constraints."));

		return Result.Success((best, bestScore));
	}
	#endregion

	#region Private — Return leg builders
	/// <summary>
	/// Builds the return leg (End → Start) for a round-trip point-to-point journey.
	/// Uses the lateral waypoint strategy with the reversed axis.
	/// </summary>
	private async Task<Result<ReturnLegDto>> BuildReturnLegAsync(RoutePlan plan, ValhallaRouteResponse outboundRaw, CancellationToken ct)
	{
		var returnCandidates = await _candidateGenerator.GenerateRoundTripReturnAsync(plan, outboundRaw, ct);

		if (returnCandidates.Count == 0)
			return Result.Failure<ReturnLegDto>(new Error("Routing.Return.None", "No return leg candidates were generated."));

		var returnScores = await Task.WhenAll(returnCandidates.Select(c => _scoringService.ScoreAsync(c, plan, ct)));

		RouteCandidate? bestReturn = null;
		var bestScore = double.NegativeInfinity;

		for (var i = 0; i < returnCandidates.Count; i++)
		{
			if (returnScores[i] > bestScore)
			{
				bestScore = returnScores[i];
				bestReturn = returnCandidates[i];
			}
		}

		if (bestReturn is null)
			return Result.Failure<ReturnLegDto>(new Error("Routing.Return.Disqualified", "All return leg candidates were disqualified."));

		var routeResult = _routeBuilder.Build(plan, bestReturn.Raw, GraphVersion);
		if (routeResult.IsFailure)
			return Result.Failure<ReturnLegDto>(routeResult.Error, routeResult.ResultExceptionType);

		return Result.Success(MapReturnLeg(routeResult.Value, bestReturn.VariantName, bestScore));
	}

	/// <summary>
	/// Builds the return leg for a DifferentRoute loop (Start → different corridor → Start).
	/// The outbound corridor is excluded via Valhalla <c>exclude_polygons</c>.
	/// </summary>
	private async Task<Result<ReturnLegDto>> BuildLoopReturnLegAsync(RoutePlan plan, ValhallaRouteResponse outboundRaw, CancellationToken ct)
	{
		var returnCandidates = await _candidateGenerator.GenerateReturnAsync(plan, outboundRaw, ct);

		if (returnCandidates.Count == 0)
			return Result.Failure<ReturnLegDto>(new Error("Routing.LoopReturn.None", "No loop return leg candidates were generated."));

		var returnScores = await Task.WhenAll(returnCandidates.Select(c => _scoringService.ScoreAsync(c, plan, ct)));

		RouteCandidate? bestReturn = null;
		var bestScore = double.NegativeInfinity;

		for (var i = 0; i < returnCandidates.Count; i++)
		{
			if (returnScores[i] > bestScore)
			{
				bestScore = returnScores[i];
				bestReturn = returnCandidates[i];
			}
		}

		if (bestReturn is null)
			return Result.Failure<ReturnLegDto>(new Error("Routing.LoopReturn.Disqualified", "All loop return candidates were disqualified."));

		var routeResult = _routeBuilder.Build(plan, bestReturn.Raw, GraphVersion);
		if (routeResult.IsFailure)
			return Result.Failure<ReturnLegDto>(routeResult.Error, routeResult.ResultExceptionType);

		return Result.Success(MapReturnLeg(routeResult.Value, bestReturn.VariantName, bestScore));
	}
	#endregion

	#region Private — ScoringProfile construction
	/// <summary>
	/// Resolves the <see cref="ScoringProfile"/> from the command's preset or custom weights.
	/// For <see cref="RoutePreset.Custom"/>, validates and constructs weights from the raw scalar inputs.
	/// For all named presets, delegates to the preset factory — no caller-supplied weights needed.
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
	#endregion

	#region Private — RoutePlan construction
	/// <summary>
	/// Builds the <see cref="RoutePlan"/> aggregate from the command's raw scalar inputs.
	/// Maps coordinates, optional loop spec, optional waypoints, and all constraint flags via their domain factories.
	/// </summary>
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
			if (dr.IsFailure)
				return Result.Failure<RoutePlan>(dr.Error, dr.ResultExceptionType);

			maxDist = dr.Value;
		}

		long? maxDurationSec = cmd.MaxDurationMinutes.HasValue ? (long)(cmd.MaxDurationMinutes.Value * 60) : null;

		var constraintsResult = RoutingConstraints.Create(
			maxDetourRatio: cmd.MaxDetourRatio,
			avoidHighways: cmd.AvoidHighways,
			avoidTolls: cmd.AvoidTolls,
			avoidFerries: cmd.AvoidFerries,
			avoidUnpaved: cmd.AvoidUnpaved,
			avoidMotorwayLinks: cmd.AvoidMotorwayLinks,
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
	/// <summary>
	/// Converts a raw fun score to a 1–5 star rating calibrated to the Ardennes/Wallonia region.
	/// </summary>
	private static int ComputeFunRating(double score) => score switch
	{
		>= 1.8 => 5,
		>= 0.9 => 4,
		>= 0.5 => 3,
		>= 0.15 => 2,
		_ => 1
	};

	/// <summary>
	/// Maps a built <see cref="Route"/> aggregate and scoring metadata to the <see cref="GenerateRouteResponse"/> DTO.
	/// Unwraps domain geometry (BoundingBox, Polyline) into client-facing primitives.
	/// </summary>
	private static GenerateRouteResponse MapToResponse(Route route, string variantName, double score, ReturnLegDto? returnLeg)
	{
		return new GenerateRouteResponse
		{
			RouteId = route.Id.Value,
			VariantName = variantName,
			FunScore = score,
			FunRating = ComputeFunRating(score),
			DistanceMeters = route.Stats.Distance.Meters,
			EstimatedDurationMinutes = (int)Math.Round((route.Stats.EstimatedDuration?.Seconds ?? 0) / 60.0),
			BoundingBox = new BoundingBoxDto(route.BoundingBox.South, route.BoundingBox.West, route.BoundingBox.North, route.BoundingBox.East),
			Polyline = route.Geometry.Points.Select(p => new double[] { p.Latitude, p.Longitude }).ToList(),
			ReturnLeg = returnLeg
		};
	}

	/// <summary>
	/// Maps a return-leg <see cref="Route"/> to a <see cref="ReturnLegDto"/>.
	/// Unwraps domain geometry (BoundingBox, Polyline) into client-facing primitives.
	/// </summary>
	private static ReturnLegDto MapReturnLeg(Route route, string variantName, double score)
	{
		return new ReturnLegDto
		{
			RouteId = route.Id.Value,
			VariantName = variantName,
			FunScore = score,
			FunRating = ComputeFunRating(score),
			DistanceMeters = route.Stats.Distance.Meters,
			EstimatedDurationMinutes = (int)Math.Round((route.Stats.EstimatedDuration?.Seconds ?? 0) / 60.0),
			BoundingBox = new BoundingBoxDto(route.BoundingBox.South, route.BoundingBox.West, route.BoundingBox.North, route.BoundingBox.East),
			Polyline = route.Geometry.Points.Select(p => new double[] { p.Latitude, p.Longitude }).ToList()
		};
	}
	#endregion
}