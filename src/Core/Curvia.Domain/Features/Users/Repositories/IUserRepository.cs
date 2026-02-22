using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.ValueObjects;

namespace Curvia.Domain.Features.Users.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for <see cref="User"/> persistence.
///              Extends base command repository with user-specific queries
///              needed by the just-in-time Keycloak provisioning middleware.
/// </summary>
public interface IUserRepository : IAsyncCommandRepository<User, UserId>
{
	/// <summary>
	/// Finds a user by their Keycloak "sub" claim.
	/// Returns null if no user with that KeycloakId exists.
	/// Used by the authentication middleware to resolve the app user from a JWT token.
	/// </summary>
	Task<User?> FindByKeycloakIdAsync(KeycloakId keycloakId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds a user by their application-level ID.
	/// Returns null if not found or soft-deleted.
	/// </summary>
	Task<User?> FindByIdAsync(UserId id, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns true if any active user has the given email address.
	/// Used for uniqueness validation during manual user creation.
	/// </summary>
	Task<bool> EmailExistsAsync(UserEmail email, CancellationToken cancellationToken = default);
}