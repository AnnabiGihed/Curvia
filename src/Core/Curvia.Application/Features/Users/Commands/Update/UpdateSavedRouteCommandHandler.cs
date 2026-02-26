using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.SavedRoutes.Commands.Update;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="UpdateSavedRouteCommand"/>.
///
///              Pipeline:
///                1. Load route via FindByIdAndUserAsync — enforces ownership.
///                2. Call aggregate.Rename() for name/notes — domain validates inputs.
///                3. Call aggregate.SetVisibility() — domain validates the enum value.
///                4. Persist via UpdateAsync.
/// </summary>
internal sealed class UpdateSavedRouteCommandHandler : ICommandHandler<UpdateSavedRouteCommand>
{
	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	public UpdateSavedRouteCommandHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result> Handle(UpdateSavedRouteCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);
		var savedRouteId = new SavedRouteId(command.SavedRouteId);

		var savedRoute = await _savedRouteRepository.FindByIdAndUserAsync(savedRouteId, userId, cancellationToken);
		if (savedRoute is null)
			return Result.Failure(new Error("SavedRoute.NotFound", "The saved route was not found or does not belong to your profile."), ResultExceptionType.NotFound);

		var renameResult = savedRoute.Rename(command.Name, command.Notes);
		if (renameResult.IsFailure)
			return renameResult;

		var visibilityResult = savedRoute.SetVisibility(command.Visibility);
		if (visibilityResult.IsFailure)
			return visibilityResult;

		await _savedRouteRepository.UpdateAsync(savedRoute, cancellationToken);

		return Result.Success();
	}
	#endregion
}