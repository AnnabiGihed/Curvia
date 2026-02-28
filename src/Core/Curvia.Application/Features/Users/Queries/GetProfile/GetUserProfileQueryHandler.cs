using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Users.Queries.GetProfile.Responses;

namespace Curvia.Application.Features.Users.Queries.GetProfile;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetUserProfileQuery"/>.
///              Loads the application user by their ID and maps to <see cref="UserProfileDto"/>.
///              The user is guaranteed to exist at this point — the JIT provisioning middleware
///              ensures creation on first login and the AppUserId is set in the request scope.
/// </summary>
internal sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
	#region Dependencies
	private readonly IUserRepository _userRepository;
	#endregion

	#region Constructor
	public GetUserProfileQueryHandler(IUserRepository userRepository)
	{
		_userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
	}
	#endregion

	#region IQueryHandler
	public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery query, CancellationToken cancellationToken)
	{
		var userId = new UserId(query.UserId);

		var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
		if (user is null)
			return Result.Failure<UserProfileDto>(new Error("User.NotFound", "The user profile was not found."), ResultExceptionType.NotFound);

		var dto = new UserProfileDto(UserId: user.Id.Value, Email: user.Email.Value, DisplayName: user.DisplayName.Value, Locale: user.Locale?.Value, MemberSinceUtc: user.Audit.CreatedOnUtc);

		return Result.Success(dto);
	}
	#endregion
}