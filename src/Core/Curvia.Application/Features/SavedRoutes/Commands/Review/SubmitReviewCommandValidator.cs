using FluentValidation;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fast-fail validation for <see cref="SubmitReviewCommand"/>.
///              Mirrors the constraints defined in ReviewRating and ReviewComment VOs.
///              FluentValidation runs before the handler — invalid commands never reach the DB.
/// </summary>
internal sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
	public SubmitReviewCommandValidator()
	{
		RuleFor(x => x.UserId)
			.NotEmpty().WithMessage("UserId is required.");

		RuleFor(x => x.SavedRouteId)
			.NotEmpty().WithMessage("SavedRouteId is required.");

		RuleFor(x => x.Rating)
			.InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

		RuleFor(x => x.Comment)
			.MaximumLength(2000).When(x => x.Comment is not null)
			.WithMessage("Comment must not exceed 2,000 characters.");
	}
}