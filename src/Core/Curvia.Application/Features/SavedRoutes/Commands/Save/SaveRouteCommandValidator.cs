using FluentValidation;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save;

internal sealed class SaveRouteCommandValidator : AbstractValidator<SaveRouteCommand>
{
	public SaveRouteCommandValidator()
	{
		RuleFor(x => x.UserId)
			.NotEmpty().WithMessage("UserId is required.");

		RuleFor(x => x.RouteId)
			.NotEmpty().WithMessage("RouteId is required.");

		RuleFor(x => x.Name)
			.NotEmpty().WithMessage("Route name is required.")
			.MaximumLength(200).WithMessage("Route name must not exceed 200 characters.");

		RuleFor(x => x.Notes)
			.MaximumLength(1000).When(x => x.Notes is not null)
			.WithMessage("Notes must not exceed 1,000 characters.");

		RuleFor(x => x.Visibility)
			.IsInEnum().WithMessage("Visibility must be 0 (Private) or 1 (Public).");
	}
}