using FluentValidation;

namespace Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : FluentValidation validator for <see cref="SyncRoadWorksCommand"/>.
///              Validates that CountryCode is a non-empty two-character ISO 3166-1 alpha-2 code.
/// </summary>
internal sealed class SyncRoadWorksCommandValidator : AbstractValidator<SyncRoadWorksCommand>
{
	/// <summary>
	/// Initialises rules for <see cref="SyncRoadWorksCommand"/>.
	/// </summary>
	public SyncRoadWorksCommandValidator()
	{
		RuleFor(x => x.CountryCode).NotEmpty().WithMessage("CountryCode is required.").Length(2).WithMessage("CountryCode must be a 2-character ISO 3166-1 alpha-2 code.");
	}
}