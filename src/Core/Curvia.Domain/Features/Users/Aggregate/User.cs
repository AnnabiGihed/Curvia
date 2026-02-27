using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.ValueObjects;

namespace Curvia.Domain.Features.Users.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents an application-level user, provisioned just-in-time from a
///              Keycloak JWT token on first login and updated on subsequent logins when
///              profile data has changed.
///
///              PRIMITIVE OBSESSION FIX: All previously raw <c>string</c> fields are now
///              strongly-typed value objects:
///                - <see cref="KeycloakSubject"/> replaces <c>string KeycloakId</c>
///                - <see cref="UserEmail"/> replaces <c>string Email</c>
///                - <see cref="DisplayName"/> replaces <c>string DisplayName</c> / <c>Username</c>
///
///              Key invariants:
///                - KeycloakSubject is immutable after provisioning (stable identity link).
///                - Email and DisplayName may be updated when the JWT contains fresh data.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
	#region Properties
	/// <summary>
	/// Keycloak subject UUID — the immutable cross-system identity link.
	/// Set once at JIT provisioning; never changed.
	/// </summary>
	public KeycloakSubject KeycloakId { get; private set; } = default!;

	/// <summary>User's e-mail address from Keycloak. Updated on login if Keycloak reports a change.</summary>
	public UserEmail Email { get; private set; } = default!;

	/// <summary>User's display name from Keycloak. Updated on login if Keycloak reports a change.</summary>
	public DisplayName DisplayName { get; private set; } = default!;

	/// <summary>
	/// BCP 47 locale tag from the Keycloak JWT (e.g. "en", "fr-BE").
	/// Null when the token does not carry a locale claim.
	/// Updated on every login via <see cref="SyncFromToken"/>.
	/// </summary>
	public string? Locale { get; private set; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private User() { }

	/// <summary>Initialises a <see cref="User"/> aggregate with all value objects.</summary>
	/// <param name="id">Strongly-typed user identifier.</param>
	/// <param name="keycloakId">Keycloak subject value object.</param>
	/// <param name="email">User e-mail value object.</param>
	/// <param name="displayName">Display name value object.</param>
	private User(UserId id, KeycloakSubject keycloakId, UserEmail email, DisplayName displayName) : base(id)
	{
		KeycloakId = keycloakId;
		Email = email;
		DisplayName = displayName;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Provisions a new <see cref="User"/> from a Keycloak JWT token on first login.
	/// All inputs are validated via value-object constructors before the aggregate is created.
	/// </summary>
	/// <param name="keycloakSubject">Keycloak 'sub' claim (UUID string). Must be a valid UUID.</param>
	/// <param name="email">User's e-mail address from the token. Must be non-empty and structurally valid.</param>
	/// <param name="displayName">User's display name from the token (preferred_username or name claim). Must be non-empty.</param>
	/// <returns>Successful result containing the provisioned <see cref="User"/>; otherwise failure.</returns>
	public static Result<User> Provision(string keycloakSubject, string email, string displayName, string? locale = null)
	{
		var subjectResult = KeycloakSubject.Create(keycloakSubject);
		if (subjectResult.IsFailure) return Result.Failure<User>(subjectResult.Error);

		var emailResult = UserEmail.Create(email);
		if (emailResult.IsFailure) return Result.Failure<User>(emailResult.Error);

		var displayNameResult = DisplayName.Create(displayName);
		if (displayNameResult.IsFailure) return Result.Failure<User>(displayNameResult.Error);

		var now = DateTime.UtcNow;
		var user = new User(new UserId(Guid.NewGuid()), subjectResult.Value, emailResult.Value, displayNameResult.Value);
		user.Locale = locale;
		user.InitializeAudit(now, keycloakSubject);
		return Result.Success(user);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates the user's mutable profile fields with fresh data from the Keycloak JWT on login.
	/// KeycloakSubject is intentionally excluded — it is immutable after provisioning.
	/// </summary>
	public Result SyncFromToken(string email, string displayName, string? locale = null)
	{
		var emailResult = UserEmail.Create(email);
		if (emailResult.IsFailure) return Result.Failure(emailResult.Error);

		var displayNameResult = DisplayName.Create(displayName);
		if (displayNameResult.IsFailure) return Result.Failure(displayNameResult.Error);

		Email = emailResult.Value;
		DisplayName = displayNameResult.Value;
		Locale = locale;
		Touch(DateTime.UtcNow, KeycloakId.Value);
		return Result.Success();
	}
	#endregion
}