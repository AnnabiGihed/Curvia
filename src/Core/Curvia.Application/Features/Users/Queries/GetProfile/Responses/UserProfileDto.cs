namespace Curvia.Application.Features.Users.Queries.GetProfile.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Flat DTO representing the authenticated user's application profile.
///              Keycloak-backed fields (email, displayName, locale) are kept in sync
///              by the JIT provisioning middleware on every authenticated request.
/// </summary>
/// <param name="UserId">Application-level user identifier.</param>
/// <param name="Email">Primary contact email. Kept in sync with Keycloak.</param>
/// <param name="DisplayName">Public display name shown in route lists and reviews.</param>
/// <param name="Locale">BCP-47 locale tag (e.g. "fr-BE"). Null when not set.</param>
/// <param name="MemberSinceUtc">UTC timestamp of the user's first login (JIT provisioning date).</param>
public sealed record UserProfileDto(Guid UserId, string Email, string DisplayName, string? Locale, DateTime MemberSinceUtc);