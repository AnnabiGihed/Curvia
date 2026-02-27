using FluentValidation;
using Curvia.Application.Features.Awareness.Commands.SyncSpeedCameras;

namespace Curvia.Infrastructure.Jobs;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : FluentValidation validator for <see cref="SyncSpeedCamerasCommand"/>.
///              Ensures the CountryCode is a non-empty, two-character uppercase string
///              before the command reaches the handler — fail-fast before any I/O.
///
///              CRITICAL FIX C-2: This file previously contained the AwarenessCleanupJob
///              class (wrong type, wrong namespace), which caused MediatR's ValidationBehavior
///              to throw InvalidOperationException on every SyncSpeedCamerasCommand dispatch.
///              AwarenessCleanupJob belongs exclusively in
///              Curvia.Infrastructure/Jobs/AwarenessCleanupJob.cs.
/// </summary>
internal sealed class SyncSpeedCamerasCommandValidator : AbstractValidator<SyncSpeedCamerasCommand>
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SyncSpeedCamerasCommandValidator"/> with
	/// all validation rules for <see cref="SyncSpeedCamerasCommand"/>.
	/// </summary>
	public SyncSpeedCamerasCommandValidator()
	{
		RuleFor(x => x.CountryCode)
			.NotEmpty()
			.WithMessage("CountryCode is required.")
			.Length(2)
			.WithMessage("CountryCode must be exactly 2 characters (ISO 3166-1 alpha-2).")
			.Matches("^[A-Za-z]{2}$")
			.WithMessage("CountryCode must contain only letters.");
	}
	#endregion
}