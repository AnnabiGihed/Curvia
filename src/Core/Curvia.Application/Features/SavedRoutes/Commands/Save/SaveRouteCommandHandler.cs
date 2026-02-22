using Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SaveRouteCommand"/>.
///
///              Pipeline:
///                1. Verify the user exists (guard against stale JWT claims).
///                2. Wrap raw command Guid into domain RouteId strong type.
///                3. Guard against duplicate saves (same user + same route).
///                4. Create the SavedRoute aggregate (VO validation happens inside).
///                5. Persist and return response with unwrapped VO primitives.
/// </summary>
internal sealed class SaveRouteCommandHandler : ICommandHandler<SaveRouteCommand, SaveRouteResponse>
{
	#region Dependencies
	private readonly IUserRepository _userRepository;
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	public SaveRouteCommandHandler(IUserRepository userRepository, ISavedRouteRepository savedRouteRepository)
	{
		_userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result<SaveRouteResponse>> Handle(SaveRouteCommand command, CancellationToken cancellationToken)
	{
		// 1. Resolve user — guard against stale JWT claims
		var userId = new UserId(command.UserId);
		var user = await _userRepository.FindByIdAsync(userId, cancellationToken);

		if (user is null)
			return Result.Failure<SaveRouteResponse>(new Error(
				"SavedRoute.User.NotFound",
				"The authenticated user was not found. Please sign out and sign in again."));

		// 2. Wrap raw Guid into domain RouteId strong type
		var routeId = new RouteId(command.RouteId);

		// 3. Duplicate guard
		var alreadySaved = await _savedRouteRepository.AlreadySavedAsync(userId, routeId, cancellationToken);
		if (alreadySaved)
			return Result.Failure<SaveRouteResponse>(new Error(
				"SavedRoute.AlreadySaved",
				"This route is already saved to your profile."));

		// 4. Create aggregate (VO validation for Name/Notes happens inside SavedRoute.Create)
		var savedRouteResult = SavedRoute.Create(
			userId: userId,
			routeId: routeId,
			name: command.Name,
			notes: command.Notes,
			visibility: command.Visibility);

		if (savedRouteResult.IsFailure)
			return Result.Failure<SaveRouteResponse>(savedRouteResult.Error, savedRouteResult.ResultExceptionType);

		var savedRoute = savedRouteResult.Value;

		// 5. Persist
		await _savedRouteRepository.AddAsync(savedRoute, cancellationToken);

		// Unwrap VO values for the response DTO
		return Result.Success(new SaveRouteResponse(
			SavedRouteId: savedRoute.Id.Value,
			RouteId: savedRoute.RouteId.Value,   // unwrap RouteId VO
			Name: savedRoute.Name.Value,      // unwrap RouteName VO
			Visibility: savedRoute.Visibility,
			CreatedAtUtc: savedRoute.Audit.CreatedOnUtc));
	}
	#endregion
}