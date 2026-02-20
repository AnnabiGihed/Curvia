using FluentValidation;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : FluentValidation validator for <see cref="GenerateRouteCommand"/>.
///              Validates coordinates, trip mode exclusivity, preset/custom weight rules,
///              all new constraints (duration, unpaved, urban tolerance) and waypoints.
/// </summary>
internal sealed class GenerateRouteCommandValidator : AbstractValidator<GenerateRouteCommand>
{
	public GenerateRouteCommandValidator()
	{
		#region Origin coordinates

		RuleFor(x => x.StartLatitude)
			.InclusiveBetween(-90, 90)
			.WithMessage("StartLatitude must be between -90 and 90.");

		RuleFor(x => x.StartLongitude)
			.InclusiveBetween(-180, 180)
			.WithMessage("StartLongitude must be between -180 and 180.");

		#endregion

		#region Trip mode — point-to-point or loop (exactly one)

		// Both end coordinates must be provided together or not at all
		RuleFor(x => x)
			.Must(x => (x.EndLatitude.HasValue && x.EndLongitude.HasValue)
					   || (!x.EndLatitude.HasValue && !x.EndLongitude.HasValue))
			.WithName("EndCoordinates")
			.WithMessage("EndLatitude and EndLongitude must both be set, or both omitted.");

		// Exactly one of: (EndLatitude + EndLongitude) or LoopTargetDistanceMeters
		RuleFor(x => x)
			.Must(x =>
			{
				var hasEnd = x.EndLatitude.HasValue && x.EndLongitude.HasValue;
				var hasLoop = x.LoopTargetDistanceMeters.HasValue;
				return hasEnd ^ hasLoop;
			})
			.WithName("TripMode")
			.WithMessage("Exactly one of (EndLatitude + EndLongitude) or LoopTargetDistanceMeters must be set.");

		// End coordinate bounds
		When(x => x.EndLatitude.HasValue, () =>
		{
			RuleFor(x => x.EndLatitude!.Value)
				.InclusiveBetween(-90, 90)
				.WithMessage("EndLatitude must be between -90 and 90.");

			RuleFor(x => x.EndLongitude!.Value)
				.InclusiveBetween(-180, 180)
				.WithMessage("EndLongitude must be between -180 and 180.");
		});

		// Loop distance bounds
		When(x => x.LoopTargetDistanceMeters.HasValue, () =>
		{
			RuleFor(x => x.LoopTargetDistanceMeters!.Value)
				.InclusiveBetween(1_000, 500_000)
				.WithMessage("LoopTargetDistanceMeters must be between 1,000 m and 500,000 m (500 km).");
		});

		// ReturnStrategy only valid for loops
		When(x => x.EndLatitude.HasValue, () =>
		{
			RuleFor(x => x.LoopReturnStrategy)
				.Equal(ReturnStrategy.SameRoute)
				.WithMessage("LoopReturnStrategy is only applicable to loop routes, not point-to-point.");
		});

		#endregion

		#region Preset and custom weights

		RuleFor(x => x.Preset)
			.IsInEnum()
			.WithMessage("Preset must be a valid RoutePreset value.");

		// Custom preset requires all weight fields
		When(x => x.Preset == RoutePreset.Custom, () =>
		{
			RuleFor(x => x.FunFactor)
				.NotNull()
				.WithMessage("FunFactor is required when Preset is Custom.")
				.InclusiveBetween(0.0, 10.0)
				.When(x => x.FunFactor.HasValue)
				.WithMessage("FunFactor must be between 0 and 10.");

			RuleFor(x => x.CurvesWeight)
				.NotNull()
				.WithMessage("CurvesWeight is required when Preset is Custom.")
				.InclusiveBetween(0.0, 1.0)
				.When(x => x.CurvesWeight.HasValue)
				.WithMessage("CurvesWeight must be between 0 and 1.");

			RuleFor(x => x.ElevationWeight)
				.NotNull()
				.WithMessage("ElevationWeight is required when Preset is Custom.")
				.InclusiveBetween(0.0, 1.0)
				.When(x => x.ElevationWeight.HasValue)
				.WithMessage("ElevationWeight must be between 0 and 1.");

			RuleFor(x => x.SceneryWeight)
				.NotNull()
				.WithMessage("SceneryWeight is required when Preset is Custom.")
				.InclusiveBetween(0.0, 1.0)
				.When(x => x.SceneryWeight.HasValue)
				.WithMessage("SceneryWeight must be between 0 and 1.");

			// At least one weight must be > 0
			RuleFor(x => x)
				.Must(x =>
					(x.CurvesWeight ?? 0) > 0
					|| (x.ElevationWeight ?? 0) > 0
					|| (x.SceneryWeight ?? 0) > 0)
				.WithName("CustomWeights")
				.WithMessage("At least one of CurvesWeight, ElevationWeight or SceneryWeight must be greater than 0.");
		});

		#endregion

		#region Constraints

		RuleFor(x => x.MaxDetourRatio)
			.InclusiveBetween(1.0, 10.0)
			.WithMessage("MaxDetourRatio must be between 1.0 and 10.0.");

		When(x => x.MaxDistanceMeters.HasValue, () =>
		{
			RuleFor(x => x.MaxDistanceMeters!.Value)
				.GreaterThan(0)
				.WithMessage("MaxDistanceMeters must be greater than 0.");
		});

		When(x => x.MaxDurationMinutes.HasValue, () =>
		{
			RuleFor(x => x.MaxDurationMinutes!.Value)
				.GreaterThan(0)
				.WithMessage("MaxDurationMinutes must be greater than 0.")
				.LessThanOrEqualTo(1440)
				.WithMessage("MaxDurationMinutes cannot exceed 1,440 (24 hours).");
		});

		RuleFor(x => x.UrbanTolerance)
			.IsInEnum()
			.WithMessage("UrbanTolerance must be a valid value (None, PassThrough or Allowed).");

		#endregion

		#region Waypoints

		When(x => x.Waypoints is not null && x.Waypoints.Count > 0, () =>
		{
			RuleFor(x => x.Waypoints)
				.Must(w => w!.Count <= 25)
				.WithMessage("A maximum of 25 waypoints is allowed.");

			RuleForEach(x => x.Waypoints)
				.ChildRules(wp =>
				{
					wp.RuleFor(w => w.Latitude)
						.InclusiveBetween(-90, 90)
						.WithMessage("Waypoint latitude must be between -90 and 90.");

					wp.RuleFor(w => w.Longitude)
						.InclusiveBetween(-180, 180)
						.WithMessage("Waypoint longitude must be between -180 and 180.");
				});
		});

		#endregion
	}
}