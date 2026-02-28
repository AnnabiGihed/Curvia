using FluentValidation;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Enums;

namespace Curvia.Application.Features.Routes.Commands.Generate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : FluentValidation validator for <see cref="GenerateRouteCommand"/>.
///              Validates coordinates, trip mode exclusivity, RoundTrip constraints,
///              preset/custom weight rules, constraints, urban tolerance, and waypoints.
///              New presets (Hilly, Scenic, MaxFun) are automatically covered by IsInEnum().
/// </summary>
internal sealed class GenerateRouteCommandValidator : AbstractValidator<GenerateRouteCommand>
{
	/// <summary>Initialises all validation rules for <see cref="GenerateRouteCommand"/>.</summary>
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
		RuleFor(x => x)
			.Must(x => (x.EndLatitude.HasValue && x.EndLongitude.HasValue) || (!x.EndLatitude.HasValue && !x.EndLongitude.HasValue))
			.WithName("EndCoordinates")
			.WithMessage("EndLatitude and EndLongitude must both be set, or both omitted.");

		RuleFor(x => x)
			.Must(x =>
			{
				var hasEnd = x.EndLatitude.HasValue && x.EndLongitude.HasValue;
				var hasLoop = x.LoopTargetDistanceMeters.HasValue;
				return hasEnd ^ hasLoop;
			})
			.WithName("TripMode")
			.WithMessage("Exactly one of (EndLatitude + EndLongitude) or LoopTargetDistanceMeters must be set.");

		When(x => x.EndLatitude.HasValue, () =>
		{
			RuleFor(x => x.EndLatitude!.Value)
				.InclusiveBetween(-90, 90)
				.WithMessage("EndLatitude must be between -90 and 90.");

			RuleFor(x => x.EndLongitude!.Value)
				.InclusiveBetween(-180, 180)
				.WithMessage("EndLongitude must be between -180 and 180.");
		});

		When(x => x.LoopTargetDistanceMeters.HasValue, () =>
		{
			RuleFor(x => x.LoopTargetDistanceMeters!.Value)
				.InclusiveBetween(1_000, 500_000)
				.WithMessage("LoopTargetDistanceMeters must be between 1,000 m and 500,000 m.");
		});

		When(x => x.EndLatitude.HasValue && x.EndLongitude.HasValue, () =>
		{
			RuleFor(x => x.LoopReturnStrategy)
				.Equal(ReturnStrategy.SameRoute)
				.WithMessage("LoopReturnStrategy is only applicable to loop routes, not point-to-point.");
		});

		When(x => x.LoopTargetDistanceMeters.HasValue, () =>
		{
			RuleFor(x => x.RoundTrip)
				.Equal(false)
				.WithMessage("RoundTrip is only applicable to point-to-point routes, not loop routes.");
		});
		#endregion

		#region Preset and custom weights
		RuleFor(x => x.Preset)
			.IsInEnum()
			.WithMessage("Preset must be a valid RoutePreset value (0=Twisty, 1=Panoramic, 2=Balanced, 3=Custom, 4=Hilly, 5=Scenic, 6=MaxFun).");

		When(x => x.Preset == RoutePreset.Custom, () =>
		{
			RuleFor(x => x.FunFactor)
				.NotNull().WithMessage("FunFactor is required when Preset is Custom.")
				.InclusiveBetween(0, 10).When(x => x.FunFactor.HasValue)
				.WithMessage("FunFactor must be between 0 and 10.");

			RuleFor(x => x.CurvesWeight)
				.NotNull().WithMessage("CurvesWeight is required when Preset is Custom.")
				.InclusiveBetween(0, 1).When(x => x.CurvesWeight.HasValue)
				.WithMessage("CurvesWeight must be between 0 and 1.");

			RuleFor(x => x.ElevationWeight)
				.NotNull().WithMessage("ElevationWeight is required when Preset is Custom.")
				.InclusiveBetween(0, 1).When(x => x.ElevationWeight.HasValue)
				.WithMessage("ElevationWeight must be between 0 and 1.");

			RuleFor(x => x.SceneryWeight)
				.NotNull().WithMessage("SceneryWeight is required when Preset is Custom.")
				.InclusiveBetween(0, 1).When(x => x.SceneryWeight.HasValue)
				.WithMessage("SceneryWeight must be between 0 and 1.");
		});
		#endregion

		#region Constraints
		RuleFor(x => x.MaxDetourRatio)
			.InclusiveBetween(1.0, 4.0)
			.WithMessage("MaxDetourRatio must be between 1.0 and 4.0.");

		When(x => x.MaxDistanceMeters.HasValue, () =>
		{
			RuleFor(x => x.MaxDistanceMeters!.Value)
				.InclusiveBetween(1_000, 2_000_000)
				.WithMessage("MaxDistanceMeters must be between 1,000 m and 2,000,000 m.");
		});

		When(x => x.MaxDurationMinutes.HasValue, () =>
		{
			RuleFor(x => x.MaxDurationMinutes!.Value)
				.InclusiveBetween(1, 1_440)
				.WithMessage("MaxDurationMinutes must be between 1 and 1,440 (24 hours).");
		});

		RuleFor(x => x.UrbanTolerance)
			.IsInEnum()
			.WithMessage("UrbanTolerance must be a valid value (0=None, 1=PassThrough, 2=Allowed).");
		#endregion

		#region Waypoints
		When(x => x.Waypoints is { Count: > 0 }, () =>
		{
			RuleFor(x => x.Waypoints)
				.Must(w => w!.Count <= 25)
				.WithMessage("A maximum of 25 waypoints is allowed.");

			RuleForEach(x => x.Waypoints)
				.ChildRules(wp =>
				{
					wp.RuleFor(x => x.Latitude)
						.InclusiveBetween(-90, 90)
						.WithMessage("Waypoint latitude must be between -90 and 90.");

					wp.RuleFor(x => x.Longitude)
						.InclusiveBetween(-180, 180)
						.WithMessage("Waypoint longitude must be between -180 and 180.");
				});
		});
		#endregion
	}
}