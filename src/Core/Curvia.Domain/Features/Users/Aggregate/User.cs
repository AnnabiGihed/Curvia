using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.ValueObjects;

namespace Curvia.Domain.Features.Users.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Application user profile aggregate root.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
	#region Properties	
	/// <summary>
	/// Optional BCP-47 locale tag (e.g. "fr-BE").
	/// Null when not set.
	/// </summary>
	public Locale? Locale { get; private set; }

	/// <summary>
	/// Primary contact email. Unique at DB level.
	/// Kept in sync with Keycloak on login.
	/// </summary>
	public UserEmail Email { get; private set; } = default!;

	/// <summary>
	/// Keycloak "sub" claim — the stable identity bridge between Keycloak and the app DB.
	/// Immutable after creation. Unique at DB level (see AppUserConfiguration).
	/// </summary>
	public KeycloakId KeycloakId { get; private set; } = default!;

	/// <summary>
	/// Public display name shown in route lists and reviews.
	/// </summary>
	public DisplayName DisplayName { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>
	/// For EF Core.
	/// </summary>
	private User() { }

	/// <summary>
	/// Initializes a new <see cref="User"/> aggregate with validated value objects.
	/// </summary>
	/// <param name="id">Aggregate identifier.</param>
	/// <param name="keycloakId">Keycloak subject identifier.</param>
	/// <param name="email">User email address.</param>
	/// <param name="displayName">Public display name.</param>
	/// <param name="locale">Optional locale tag.</param>
	private User(UserId id, KeycloakId keycloakId, UserEmail email, DisplayName displayName, Locale? locale)
		: base(id)
	{
		Email = email;
		Locale = locale;
		KeycloakId = keycloakId;
		DisplayName = displayName;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="User"/> from profile data.
	/// </summary>
	/// <param name="keycloakId">Keycloak subject (sub claim) as string.</param>
	/// <param name="email">Raw email input.</param>
	/// <param name="displayName">Raw display name input.</param>
	/// <param name="locale">Optional raw locale input.</param>
	/// <returns>A successful result containing the created <see cref="User"/>, or a failure with a domain error.</returns>
	public static Result<User> Create(string keycloakId, string email, string displayName, string? locale = null)
	{
		var keycloakIdResult = KeycloakId.Create(keycloakId);
		if (keycloakIdResult.IsFailure)
			return Result.Failure<User>(keycloakIdResult.Error, keycloakIdResult.ResultExceptionType);

		var emailResult = UserEmail.Create(email);
		if (emailResult.IsFailure)
			return Result.Failure<User>(emailResult.Error, emailResult.ResultExceptionType);

		var displayNameResult = DisplayName.Create(displayName);
		if (displayNameResult.IsFailure)
			return Result.Failure<User>(displayNameResult.Error, displayNameResult.ResultExceptionType);

		Locale? localeVo = null;
		if (locale is not null)
		{
			var localeResult = Locale.Create(locale);
			if (localeResult.IsFailure)
				return Result.Failure<User>(localeResult.Error, localeResult.ResultExceptionType);

			localeVo = localeResult.Value;
		}

		var user = new User(new UserId(Guid.NewGuid()), keycloakIdResult.Value, emailResult.Value, displayNameResult.Value, localeVo);

		return Result.Success(user);
	}
	#endregion

	#region Domain behaviours
	/// <summary>
	/// Soft-deletes the user.
	/// </summary>
	/// <param name="actorId">Identifier of the actor performing the deactivation.</param>
	public void Deactivate(string actorId)
	{
		Delete(DateTime.UtcNow, actorId);
	}

	/// <summary>
	/// Stamps the real Keycloak sub claim onto a manually-created user.
	/// One-way operation — cannot change a real KeycloakId once set.
	/// </summary>
	/// <param name="keycloakId">Keycloak subject (sub claim) as string.</param>
	/// <returns>Success when the KeycloakId is updated; otherwise failure with domain error.</returns>
	public Result UpdateKeycloakId(string keycloakId)
	{
		var result = KeycloakId.Create(keycloakId);
		if (result.IsFailure)
			return Result.Failure(result.Error, result.ResultExceptionType);

		KeycloakId = result.Value;
		return Result.Success();
	}

	/// <summary>
	/// Updates profile fields from a refreshed Keycloak JWT.
	/// Called by just-in-time provisioning middleware on each authenticated request.
	/// </summary>
	/// <param name="email">Raw email input from token/identity provider.</param>
	/// <param name="displayName">Raw display name input from token/identity provider.</param>
	/// <param name="locale">Optional locale input from token/identity provider.</param>
	/// <returns>Success when the profile is updated; otherwise failure with domain error.</returns>
	public Result UpdateProfile(string email, string displayName, string? locale)
	{
		var emailResult = UserEmail.Create(email);
		if (emailResult.IsFailure)
			return Result.Failure(emailResult.Error, emailResult.ResultExceptionType);

		var displayNameResult = DisplayName.Create(displayName);
		if (displayNameResult.IsFailure)
			return Result.Failure(displayNameResult.Error, displayNameResult.ResultExceptionType);

		Locale? localeVo = null;
		if (locale is not null)
		{
			var localeResult = Locale.Create(locale);
			if (localeResult.IsFailure)
				return Result.Failure(localeResult.Error, localeResult.ResultExceptionType);
			localeVo = localeResult.Value;
		}

		Locale = localeVo;
		Email = emailResult.Value;
		DisplayName = displayNameResult.Value;

		return Result.Success();
	}
	#endregion
}