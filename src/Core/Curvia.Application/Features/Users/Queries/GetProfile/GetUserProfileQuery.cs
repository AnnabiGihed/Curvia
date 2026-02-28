using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Users.Queries.GetProfile.Responses;

namespace Curvia.Application.Features.Users.Queries.GetProfile;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Returns the application profile for the authenticated user.
///              Profile fields are kept in sync with Keycloak by the JIT provisioning middleware.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;