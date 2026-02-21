using FluentValidation;

namespace Curvia.Application.Features.Routing.Routes.Commands.ExportGpx;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : FluentValidation validator for <see cref="ExportGpxCommand"/>.
///              Validates polyline integrity before the handler attempts XML construction.
/// </summary>
internal sealed class ExportGpxCommandValidator : AbstractValidator<ExportGpxCommand>
{
	public ExportGpxCommandValidator()
	{
		#region Route name
		When(x => x.RouteName is not null, () =>
		{
			RuleFor(x => x.RouteName)
				.MaximumLength(100)
				.WithMessage("RouteName must not exceed 100 characters.");
		});
		#endregion

		#region Outbound polyline
		RuleFor(x => x.Polyline)
			.NotNull()
			.WithMessage("Polyline is required.")
			.Must(p => p.Count >= 2)
			.WithMessage("Polyline must contain at least 2 points.");

		RuleForEach(x => x.Polyline)
			.Must(pt => pt.Length >= 2)
			.WithMessage("Each polyline point must be a [latitude, longitude] pair.")
			.Must(pt => pt[0] is >= -90.0 and <= 90.0)
			.WithMessage("Polyline contains a point with an invalid latitude (must be −90 to 90).")
			.Must(pt => pt.Length < 2 || pt[1] is >= -180.0 and <= 180.0)
			.WithMessage("Polyline contains a point with an invalid longitude (must be −180 to 180).");

		RuleFor(x => x.DistanceMeters)
			.GreaterThan(0)
			.WithMessage("DistanceMeters must be greater than 0.");

		RuleFor(x => x.EstimatedDurationMinutes)
			.GreaterThan(0)
			.WithMessage("EstimatedDurationMinutes must be greater than 0.");

		RuleFor(x => x.FunRating)
			.InclusiveBetween(1, 5)
			.WithMessage("FunRating must be between 1 and 5.");
		#endregion

		#region Return leg (optional)
		When(x => x.ReturnLegPolyline is { Count: > 0 }, () =>
		{
			RuleFor(x => x.ReturnLegPolyline)
				.Must(p => p!.Count >= 2)
				.WithMessage("ReturnLegPolyline must contain at least 2 points when provided.");

			RuleForEach(x => x.ReturnLegPolyline)
				.Must(pt => pt.Length >= 2)
				.WithMessage("Each return leg polyline point must be a [latitude, longitude] pair.");

			RuleFor(x => x.ReturnLegDistanceMeters)
				.GreaterThan(0)
				.WithMessage("ReturnLegDistanceMeters must be greater than 0 when a return leg is provided.");

			RuleFor(x => x.ReturnLegDurationMinutes)
				.GreaterThan(0)
				.WithMessage("ReturnLegDurationMinutes must be greater than 0 when a return leg is provided.");

			RuleFor(x => x.ReturnLegFunRating)
				.InclusiveBetween(1, 5)
				.WithMessage("ReturnLegFunRating must be between 1 and 5 when a return leg is provided.");
		});
		#endregion
	}
}