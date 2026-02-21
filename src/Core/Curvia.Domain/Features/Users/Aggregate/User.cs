using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;

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
///                1. User authenticates via Keycloak → receives JWT.
///                2. JWT contains the "sub" claim (a UUID string) = KeycloakId.
///                3. API middleware reads the sub claim, looks up AppUser by KeycloakId.
///                4. If not found → auto-creates AppUser from JWT claims (email,
///                   preferred_username). This is the "just-in-time provisioning" pattern.
///                5. AppUser.Id is injected into the request context for downstream use.
///
///              Before Keycloak:
///                Users are seeded manually or created via an admin endpoint.
///                KeycloakId can be set to a placeholder GUID string.
///                When Keycloak is connected, the placeholder is updated to the real sub claim.
///
///              IDENTITY SEPARATION (why not store everything in Keycloak):
///                - Application data (saved routes, reviews, preferences) must survive
///                  identity provider changes without data migration.
///                - Cross-domain queries (e.g., "routes saved by users in Belgium")
///                  are significantly simpler against the app DB than the Keycloak API.
///                - Soft delete, audit, and business rules belong to the domain layer.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
	#region Properties
	/// <summary>
	/// Optional locale preference (e.g., "fr-BE", "nl-BE", "en-US").
	/// Used for UI localisation — not enforced by the domain, stored as-is.
	/// </summary>
	public string? Locale { get; private set; }

	/// <summary>
	/// Primary contact email sourced from the Keycloak "email" claim.
	/// Kept in sync with Keycloak on login via UpdateProfile().
	/// Max 320 chars (RFC 5321 limit).
	/// </summary>
	public string Email { get; private set; } = default!;

	/// <summary>
	/// The Keycloak "sub" claim value — the stable, unique identifier issued by Keycloak.
	/// Format: UUID string (e.g., "a3f1c2d4-9e1b-4c7a-bb82-f1234567890a").
	///
	/// UNIQUENESS: unique index enforced at DB level (see AppUserConfiguration).
	/// IMMUTABILITY: set once at creation; Keycloak never changes the sub claim.
	/// PRE-KEYCLOAK: set to a deterministic placeholder so the app works without Keycloak.
	///              When Keycloak is connected, call UpdateKeycloakId() to stamp the real value.
	/// </summary>
	public string KeycloakId { get; private set; } = default!;

	/// <summary>
	/// Public display name shown in route lists and reviews.
	/// Sourced from Keycloak "preferred_username" claim on first login.
	/// User can override via profile settings.
	/// Max 100 chars.
	/// </summary>
	public string DisplayName { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>For EF Core.</summary>
	private User() { }

	private User(UserId id, string keycloakId, string email, string displayName, string? locale)
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
	/// <param name="keycloakId">
	/// The Keycloak "sub" claim. Use a deterministic placeholder UUID string
	/// (e.g., <c>Guid.NewGuid().ToString()</c>) when Keycloak is not yet wired.
	/// </param>
	/// <param name="email">Contact email. Must be non-empty, max 320 chars.</param>
	/// <param name="displayName">Public display name. Must be non-empty, max 100 chars.</param>
	/// <param name="locale">Optional BCP-47 locale tag (e.g., "fr-BE").</param>
	public static Result<User> Create(string keycloakId, string email, string displayName, string? locale = null)
	{
		if (string.IsNullOrWhiteSpace(keycloakId))
			return Result.Failure<User>(new Error("User.KeycloakId.Empty", "KeycloakId must not be empty."));

		if (string.IsNullOrWhiteSpace(email))
			return Result.Failure<User>(new Error("User.Email.Empty", "Email must not be empty."));

		if (email.Length > 320)
			return Result.Failure<User>(new Error("User.Email.TooLong", "Email must not exceed 320 characters."));

		if (string.IsNullOrWhiteSpace(displayName))
			return Result.Failure<User>(new Error("User.DisplayName.Empty", "DisplayName must not be empty."));

		if (displayName.Length > 100)
			return Result.Failure<User>(new Error("User.DisplayName.TooLong", "DisplayName must not exceed 100 characters."));

		var user = new User(new UserId(Guid.NewGuid()), keycloakId.Trim(), email.Trim().ToLowerInvariant(), displayName.Trim(), locale?.Trim());

		return Result.Success(user);
	}
	#endregion

	#region Domain behaviours
	/// <summary>
	/// Updates the email and display name from a refreshed Keycloak token.
	/// Called by the just-in-time provisioning middleware on each authenticated request.
	/// </summary>
	public Result UpdateProfile(string email, string displayName, string? locale)
	{
		if (string.IsNullOrWhiteSpace(email))
			return Result.Failure(new Error("User.Email.Empty", "Email must not be empty."));

		if (email.Length > 320)
			return Result.Failure(new Error("User.Email.TooLong", "Email must not exceed 320 characters."));

		if (string.IsNullOrWhiteSpace(displayName))
			return Result.Failure(new Error("User.DisplayName.Empty", "DisplayName must not be empty."));

		if (displayName.Length > 100)
			return Result.Failure(new Error("User.DisplayName.TooLong", "DisplayName must not exceed 100 characters."));

		Email = email.Trim().ToLowerInvariant();
		DisplayName = displayName.Trim();
		Locale = locale?.Trim();

		return Result.Success();
	}

	/// <summary>
	/// Stamps the real Keycloak sub claim onto a manually-created user.
	/// Called once when Keycloak is first connected and the placeholder is replaced.
	/// This is a one-way operation — you cannot change a real KeycloakId.
	/// </summary>
	public Result UpdateKeycloakId(string keycloakId)
	{
		if (string.IsNullOrWhiteSpace(keycloakId))
			return Result.Failure(new Error("User.KeycloakId.Empty", "KeycloakId must not be empty."));

		KeycloakId = keycloakId.Trim();
		return Result.Success();
	}

	/// <summary>
	/// Soft-deletes the user. All associated SavedRoutes remain in the DB
	/// but are excluded from queries via global query filters.
	/// </summary>
	public void Deactivate(string actorId) => Delete(DateTime.UtcNow, actorId);
	#endregion
}