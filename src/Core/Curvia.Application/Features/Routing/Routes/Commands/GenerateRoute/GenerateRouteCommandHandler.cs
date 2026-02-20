using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Orchestrates the V1 route generation pipeline:
///              1. Build domain RoutePlan from command inputs
///              2. Generate Valhalla route candidates (3 variants)
///              3. Score each candidate using RouteScoringService
///              4. Pick the highest-scoring valid candidate
///              5. Build the Route domain aggregate from the winner
///              6. Map to GenerateRouteResponse DTO
///
///              Persistence is intentionally excluded in V1.
///              The graph version is pinned to "v1" until the worker pipeline is live.
/// </summary>
internal sealed class GenerateRouteCommandHandler : ICommandHandler<GenerateRouteCommand, GenerateRouteResponse>
{
	#region Constants
	private const string GraphVersion = "v1";
	#endregion

	#region Dependencies
	private readonly IRouteBuilder _routeBuilder;
	private readonly IRouteScoringService _scoringService;
	private readonly IRouteCandidateGenerator _candidateGenerator;
	#endregion

	#region Constructor
	public GenerateRouteCommandHandler(IRouteCandidateGenerator candidateGenerator, IRouteScoringService scoringService, IRouteBuilder routeBuilder)
	{
		_routeBuilder = routeBuilder ?? throw new ArgumentNullException(nameof(routeBuilder));
		_scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
		_candidateGenerator = candidateGenerator ?? throw new ArgumentNullException(nameof(candidateGenerator));
	}
	#endregion

	#region ICommandHandler implementation
	public async Task<Result<GenerateRouteResponse>> Handle(GenerateRouteCommand command, CancellationToken cancellationToken)
	{
		#region Build domain RoutePlan
		var planResult = BuildRoutePlan(command);
		if (planResult.IsFailure)
			return Result.Failure<GenerateRouteResponse>(planResult.Error, planResult.ResultExceptionType);

		var plan = planResult.Value;
		#endregion

		#region Generate candidates from Valhalla
		IReadOnlyList<RouteCandidate> candidates;
		try
		{
			candidates = await _candidateGenerator.GenerateAsync(plan, cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Failure<GenerateRouteResponse>(new Error("Routing.Valhalla.Unavailable", $"Valhalla routing engine failed: {ex.Message}"), ResultExceptionType.BadRequest);
		}

		if (candidates.Count == 0)
			return Result.Failure<GenerateRouteResponse>(new Error("Routing.NoCandidates", "No route candidates were returned by the routing engine."), ResultExceptionType.BadRequest);
		#endregion

		#region Score each candidate
		var scored = candidates.Select(c => new RouteScoredCandidate(c, _scoringService.Score(c.Raw, plan, c.VariantName))).ToList();
		#endregion

		#region Pick winner (highest score above disqualification threshold)
		var winner = scored
			.Where(s => s.Score > -999_999)
			.OrderByDescending(s => s.Score)
			.FirstOrDefault();

		if (winner is null)
			return Result.Failure<GenerateRouteResponse>(new Error("Routing.AllCandidatesDisqualified", "All route candidates violated routing constraints."), ResultExceptionType.BadRequest);
		#endregion

		#region Build Route aggregate
		var routeResult = _routeBuilder.Build(plan, winner.Candidate.Raw, GraphVersion);
		if (routeResult.IsFailure)
			return Result.Failure<GenerateRouteResponse>(routeResult.Error, routeResult.ResultExceptionType);

		var route = routeResult.Value;
		#endregion

		#region Map to response DTO
		var polyline = route.Geometry.Points
			.Select(p => new CoordinateResponse(p.Latitude, p.Longitude))
			.ToList();

		var bbox = new BoundingBoxResponse(route.BoundingBox.MinLatitude, route.BoundingBox.MinLongitude, route.BoundingBox.MaxLatitude, route.BoundingBox.MaxLongitude);

		var response = new GenerateRouteResponse(RouteId: route.Id.Value, VariantName: winner.Candidate.VariantName, DistanceMeters: route.Stats.Distance.Meters, DurationSeconds: route.Stats.EstimatedDuration?.Seconds ?? 0, FunScore: Math.Round(winner.Score, 4), Polyline: polyline, BoundingBox: bbox);
		#endregion

		return Result.Success(response);
	}
	#endregion


	#region Private helper
	private static Result<RoutePlan> BuildRoutePlan(GenerateRouteCommand cmd)
	{

		var startResult = GeoCoordinate.Create(cmd.StartLatitude, cmd.StartLongitude);
		if (startResult.IsFailure)
			return Result.Failure<RoutePlan>(startResult.Error, startResult.ResultExceptionType);

		#region End coordinate (point-to-point) or null (loop)
		GeoCoordinate? end = null;
		if (cmd.EndLatitude.HasValue && cmd.EndLongitude.HasValue)
		{
			var endResult = GeoCoordinate.Create(cmd.EndLatitude.Value, cmd.EndLongitude.Value);
			if (endResult.IsFailure)
				return Result.Failure<RoutePlan>(endResult.Error, endResult.ResultExceptionType);
			end = endResult.Value;
		}
		#endregion

		#region LoopSpec (loop mode)
		LoopSpec? loopSpec = null;
		if (cmd.LoopTargetDistanceMeters.HasValue)
		{
			var distResult = Distance.Create(cmd.LoopTargetDistanceMeters.Value);
			if (distResult.IsFailure)
				return Result.Failure<RoutePlan>(distResult.Error, distResult.ResultExceptionType);

			var loopResult = LoopSpec.Create(distResult.Value);
			if (loopResult.IsFailure)
				return Result.Failure<RoutePlan>(loopResult.Error, loopResult.ResultExceptionType);

			loopSpec = loopResult.Value;
		}
		#endregion

		#region Waypoints
		List<Waypoint>? waypoints = null;
		if (cmd.Waypoints is { Count: > 0 })
		{
			waypoints = new List<Waypoint>(cmd.Waypoints.Count);
			foreach (var wp in cmd.Waypoints)
			{
				var gcResult = GeoCoordinate.Create(wp.Latitude, wp.Longitude);
				if (gcResult.IsFailure)
					return Result.Failure<RoutePlan>(gcResult.Error, gcResult.ResultExceptionType);

				var wpResult = Waypoint.Create(gcResult.Value);
				if (wpResult.IsFailure)
					return Result.Failure<RoutePlan>(wpResult.Error, wpResult.ResultExceptionType);

				waypoints.Add(wpResult.Value);
			}
		}
		#endregion

		#region MaxDistance constraint (optional)
		Distance? maxDistance = null;
		if (cmd.MaxDistanceMeters.HasValue)
		{
			var mdResult = Distance.Create(cmd.MaxDistanceMeters.Value);
			if (mdResult.IsFailure)
				return Result.Failure<RoutePlan>(mdResult.Error, mdResult.ResultExceptionType);

			maxDistance = mdResult.Value;
		}
		#endregion

		#region RoutingConstraints
		var constraintsResult = RoutingConstraints.Create(cmd.MaxDetourRatio, cmd.AvoidHighways, cmd.AvoidTolls, maxDistance);
		if (constraintsResult.IsFailure)
			return Result.Failure<RoutePlan>(constraintsResult.Error, constraintsResult.ResultExceptionType);
		#endregion

		#region ScoringWeights
		var weightsResult = ScoringWeights.Create(cmd.CurvesWeight, cmd.ElevationWeight, cmd.SceneryWeight);
		if (weightsResult.IsFailure)
			return Result.Failure<RoutePlan>(weightsResult.Error, weightsResult.ResultExceptionType);
		#endregion

		#region ScoringProfile
		var profileResult = ScoringProfile.Create(cmd.FunFactor, weightsResult.Value);
		if (profileResult.IsFailure)
			return Result.Failure<RoutePlan>(profileResult.Error, profileResult.ResultExceptionType);
		#endregion

		#region RoutePlan aggregate
		return RoutePlan.Create(start: startResult.Value, end: end,	loopSpec: loopSpec,	waypoints: waypoints, constraints: constraintsResult.Value, scoringProfile: profileResult.Value);
		#endregion
	}
	#endregion
}