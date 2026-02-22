using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SaveRouteCommand"/>.
/// </summary>
internal sealed class SaveRouteCommandHandler : ICommandHandler<SaveRouteCommand, SaveRouteResponse>
{
	#region Dependencies
	private readonly IUserRepository _userRepository;
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SaveRouteCommandHandler"/>.
	/// </summary>
	/// <param name="userRepository">Repository used to validate the authenticated user exists.</param>
	/// <param name="savedRouteRepository">Repository used to persist saved routes and check duplicates.</param>
	public SaveRouteCommandHandler(IUserRepository userRepository, ISavedRouteRepository savedRouteRepository)
	{
		_userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Saves a generated route into the authenticated user's profile.
	/// Enforces:
	/// - user existence
	/// - uniqueness per (UserId, RouteId)
	/// - domain validation via <see cref="SavedRoute.Create"/>
	/// </summary>
	/// <param name="command">Command containing route data to persist.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A <see cref="SaveRouteResponse"/> describing the newly saved route.</returns>
	public async Task<Result<SaveRouteResponse>> Handle(SaveRouteCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);
		var user = await _userRepository.FindByIdAsync(userId, cancellationToken);

		if (user is null)
			return Result.Failure<SaveRouteResponse>(new Error("SavedRoute.User.NotFound", "The authenticated user was not found. Please sign out and sign in again."));

		var routeId = new RouteId(command.RouteId);

		var alreadySaved = await _savedRouteRepository.AlreadySavedAsync(userId, routeId, cancellationToken);
		if (alreadySaved)
			return Result.Failure<SaveRouteResponse>(new Error("SavedRoute.AlreadySaved", "This route is already saved to your profile."));

		var savedRouteResult = SavedRoute.Create(userId: userId, routeId: routeId, name: command.Name, notes: command.Notes, visibility: command.Visibility);

		if (savedRouteResult.IsFailure)
			return Result.Failure<SaveRouteResponse>(savedRouteResult.Error, savedRouteResult.ResultExceptionType);

		var savedRoute = savedRouteResult.Value;

		await _savedRouteRepository.AddAsync(savedRoute, cancellationToken);

		return Result.Success(new SaveRouteResponse(SavedRouteId: savedRoute.Id.Value, RouteId: savedRoute.RouteId.Value, Name: savedRoute.Name.Value, Visibility: savedRoute.Visibility, CreatedAtUtc: savedRoute.Audit.CreatedOnUtc));
	}
	#endregion
}