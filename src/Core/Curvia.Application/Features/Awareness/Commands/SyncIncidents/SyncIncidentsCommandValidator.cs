using FluentValidation;

namespace Curvia.Application.Features.Awareness.Commands.SyncIncidents;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : FluentValidation validator for <see cref="SyncIncidentsCommand"/>.
///              Validates that CountryCode is a non-empty two-character ISO 3166-1 alpha-2 code.
/// </summary>
internal sealed class SyncIncidentsCommandValidator : AbstractValidator<SyncIncidentsCommand>
{
	/// <summary>
	/// Initialises rules for <see cref="SyncIncidentsCommand"/>.
	/// </summary>
	public SyncIncidentsCommandValidator()
	{
		RuleFor(x => x.CountryCode).NotEmpty().WithMessage("CountryCode is required.").Length(2).WithMessage("CountryCode must be a 2-character ISO 3166-1 alpha-2 code.");
	}
}