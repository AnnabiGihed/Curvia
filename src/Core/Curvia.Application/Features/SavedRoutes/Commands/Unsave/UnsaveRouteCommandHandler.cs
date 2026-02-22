using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.SavedRoutes.Commands.Unsave;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="UnsaveRouteCommand"/>.
///
///              Pipeline:
///                1. Load the saved route via FindByIdAndUserAsync — enforces ownership
///                   in a single query (returns null when not owned by this user).
///                2. Call aggregate.Remove() — soft-deletes the route and stamps audit.
///                3. Persist via UpdateAsync.
///
///              WHY FindByIdAndUserAsync:
///              A user must only be able to unsave their own routes. Using the ownership-
///              aware query means we never need to explicitly compare UserId in the handler —
///              the repository predicate handles it, and null == not found == not yours.
/// </summary>
internal sealed class UnsaveRouteCommandHandler : ICommandHandler<UnsaveRouteCommand>
{
	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="UnsaveRouteCommandHandler"/>.
	/// </summary>
	/// <param name="savedRouteRepository">Repository used to load and persist saved routes.</param>
	public UnsaveRouteCommandHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Soft-deletes the saved route identified by the command.
	/// Only the owner can unsave their own route.
	/// </summary>
	/// <param name="command">Command containing the authenticated user id and saved route id.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Success when the route is removed; NotFound when the route does not exist or is not owned by this user.</returns>
	public async Task<Result> Handle(UnsaveRouteCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);
		var savedRouteId = new SavedRouteId(command.SavedRouteId);

		var savedRoute = await _savedRouteRepository.FindByIdAndUserAsync(savedRouteId, userId, cancellationToken);
		if (savedRoute is null)
			return Result.Failure(new Error("SavedRoute.NotFound", "The saved route was not found or does not belong to your profile."), ResultExceptionType.NotFound);

		savedRoute.Remove(command.UserId.ToString());

		await _savedRouteRepository.UpdateAsync(savedRoute, cancellationToken);

		return Result.Success();
	}
	#endregion
}