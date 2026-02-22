using Curvia.Domain.Features.Users.ValueObjects;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Application user profile aggregate root.
///
///              KEYCLOAK INTEGRATION STRATEGY:
///              ─────────────────────────────────────────────────────────────────
///              Keycloak is the identity provider — it owns credentials and sessions.
///              This aggregate owns the application-level profile (preferences, saved
///              routes, reviews). The two are linked by a single column: KeycloakId.
///
///              Flow after Keycloak is wired:
///                1. User authenticates → receives JWT.
///                2. JWT contains the "sub" claim = KeycloakId.Value.
///                3. API middleware reads sub, looks up User by KeycloakId.
///                4. If not found → auto-creates User from JWT claims (email,
///                   preferred_username). This is the "just-in-time provisioning" pattern.
///                5. User.Id is injected into the request context for downstream use.
///
///              VALUE OBJECTS:
///                All string fields are value objects — validation lives in the VO,
///                not scattered across the aggregate and command validators.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
	#region Properties

	/// <summary>
	/// Keycloak "sub" claim — the stable identity bridge between Keycloak and the app DB.
	/// Immutable after creation. Unique at DB level (see AppUserConfiguration).
	/// </summary>
	public KeycloakId KeycloakId { get; private set; } = default!;

	/// <summary>Primary contact email. Unique at DB level. Kept in sync with Keycloak on login.</summary>
	public UserEmail Email { get; private set; } = default!;

	/// <summary>Public display name shown in route lists and reviews.</summary>
	public DisplayName DisplayName { get; private set; } = default!;

	/// <summary>Optional BCP-47 locale tag (e.g. "fr-BE"). Null when not set.</summary>
	public Locale? Locale { get; private set; }

	#endregion

	#region Constructors

	/// <summary>For EF Core.</summary>
	private User() { }

	private User(UserId id, KeycloakId keycloakId, UserEmail email, DisplayName displayName, Locale? locale)
		: base(id)
	{
		KeycloakId = keycloakId;
		Email = email;
		DisplayName = displayName;
		Locale = locale;
	}

	#endregion

	#region Factory

	/// <summary>
	/// Creates a new <see cref="User"/> from profile data.
	/// </summary>
	public static Result<User> Create(
		string keycloakId,
		string email,
		string displayName,
		string? locale = null)
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
			var localeResult = ValueObjects.Locale.Create(locale);
			if (localeResult.IsFailure)
				return Result.Failure<User>(localeResult.Error, localeResult.ResultExceptionType);
			localeVo = localeResult.Value;
		}

		var id = new UserId(Guid.NewGuid());
		var user = new User(id, keycloakIdResult.Value, emailResult.Value, displayNameResult.Value, localeVo);

		return Result.Success(user);
	}

	#endregion

	#region Domain behaviours

	/// <summary>
	/// Updates profile fields from a refreshed Keycloak JWT.
	/// Called by just-in-time provisioning middleware on each authenticated request.
	/// </summary>
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

		Email = emailResult.Value;
		DisplayName = displayNameResult.Value;
		Locale = localeVo;

		return Result.Success();
	}

	/// <summary>
	/// Stamps the real Keycloak sub claim onto a manually-created user.
	/// One-way operation — cannot change a real KeycloakId once set.
	/// </summary>
	public Result UpdateKeycloakId(string keycloakId)
	{
		var result = KeycloakId.Create(keycloakId);
		if (result.IsFailure)
			return Result.Failure(result.Error, result.ResultExceptionType);

		KeycloakId = result.Value;
		return Result.Success();
	}

	/// <summary>Soft-deletes the user.</summary>
	public void Deactivate(string actorId) => Delete(DateTime.UtcNow, actorId);

	#endregion
}