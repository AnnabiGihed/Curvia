using FluentValidation;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

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