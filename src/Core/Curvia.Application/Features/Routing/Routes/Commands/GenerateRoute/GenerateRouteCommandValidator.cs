using FluentValidation;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : FluentValidation validator for GenerateRouteCommand.
///              Enforces all input constraints before the command reaches the handler.
///              Domain-level validation is still enforced inside the aggregate factories.
/// </summary>
public sealed class GenerateRouteCommandValidator : AbstractValidator<GenerateRouteCommand>
{
	#region Constructor
	public GenerateRouteCommandValidator()
	{
		#region Start coordinate
		RuleFor(x => x.StartLatitude)
			.InclusiveBetween(-90.0, 90.0)
			.WithMessage("StartLatitude must be between -90 and 90.");

		RuleFor(x => x.StartLongitude)
			.InclusiveBetween(-180.0, 180.0)
			.WithMessage("StartLongitude must be between -180 and 180.");
		#endregion

		#region Mode: point-to-point XOR loop
		RuleFor(x => x)
			.Must(x => HasEnd(x) ^ HasLoop(x))
			.WithName("RoutingMode")
			.WithMessage("Exactly one of (EndLatitude + EndLongitude) or (LoopTargetDistanceMeters) must be provided.");
		#endregion

		#region End coordinate (only when point-to-point)
		When(x => HasEnd(x), () =>
		{
			RuleFor(x => x.EndLatitude!.Value)
				.InclusiveBetween(-90.0, 90.0)
				.WithMessage("EndLatitude must be between -90 and 90.");

			RuleFor(x => x.EndLongitude!.Value)
				.InclusiveBetween(-180.0, 180.0)
				.WithMessage("EndLongitude must be between -180 and 180.");
		});
		#endregion

		#region Loop distance (only when loop mode)
		When(x => HasLoop(x), () =>
		{
			RuleFor(x => x.LoopTargetDistanceMeters!.Value)
				.GreaterThanOrEqualTo(1_000)
				.WithMessage("LoopTargetDistanceMeters must be at least 1,000 m (1 km).")
				.LessThanOrEqualTo(500_000)
				.WithMessage("LoopTargetDistanceMeters must not exceed 500,000 m (500 km).");
		});
		#endregion

		#region Waypoints
		When(x => x.Waypoints is { Count: > 0 }, () =>
		{
			RuleFor(x => x.Waypoints!.Count)
				.LessThanOrEqualTo(25)
				.WithMessage("A maximum of 25 waypoints is allowed.");

			RuleForEach(x => x.Waypoints)
				.ChildRules(wp =>
				{
					wp.RuleFor(w => w.Latitude)
						.InclusiveBetween(-90.0, 90.0)
						.WithMessage("Waypoint Latitude must be between -90 and 90.");

					wp.RuleFor(w => w.Longitude)
						.InclusiveBetween(-180.0, 180.0)
						.WithMessage("Waypoint Longitude must be between -180 and 180.");
				});
		});
		#endregion

		#region Constraints
		RuleFor(x => x.MaxDetourRatio)
			.GreaterThanOrEqualTo(1.0)
			.WithMessage("MaxDetourRatio must be at least 1.0 (no detour).")
			.LessThanOrEqualTo(10.0)
			.WithMessage("MaxDetourRatio must not exceed 10.0.");

		When(x => x.MaxDistanceMeters.HasValue, () =>
		{
			RuleFor(x => x.MaxDistanceMeters!.Value)
				.GreaterThan(0)
				.WithMessage("MaxDistanceMeters must be greater than 0.");
		});
		#endregion

		#region Scoring profile
		RuleFor(x => x.FunFactor)
			.InclusiveBetween(0.0, 10.0)
			.WithMessage("FunFactor must be between 0 and 10.");

		RuleFor(x => x.CurvesWeight)
			.InclusiveBetween(0.0, 1.0)
			.WithMessage("CurvesWeight must be between 0 and 1.");

		RuleFor(x => x.ElevationWeight)
			.InclusiveBetween(0.0, 1.0)
			.WithMessage("ElevationWeight must be between 0 and 1.");

		RuleFor(x => x.SceneryWeight)
			.InclusiveBetween(0.0, 1.0)
			.WithMessage("SceneryWeight must be between 0 and 1.");

		RuleFor(x => x)
			.Must(x => (x.CurvesWeight + x.ElevationWeight + x.SceneryWeight) > 0)
			.WithName("ScoringWeights")
			.WithMessage("At least one scoring weight (Curves, Elevation, Scenery) must be greater than 0.");
		#endregion
	}
	#endregion

	#region Private helper
	private static bool HasEnd(GenerateRouteCommand x) =>
		x.EndLatitude.HasValue && x.EndLongitude.HasValue;

	private static bool HasLoop(GenerateRouteCommand x) =>
		x.LoopTargetDistanceMeters.HasValue;
	#endregion
}