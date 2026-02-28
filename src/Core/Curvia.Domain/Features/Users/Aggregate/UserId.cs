using Pivot.Framework.Domain.Primitives;

namespace Curvia.Domain.Features.Users.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed GUID identifier for the <see cref="User"/> aggregate root.
///              Distinct from the Keycloak identity — the app owns this ID.
///              The Keycloak subject claim (<c>KeycloakId</c>) is stored as a separate column.
/// </summary>
public sealed record UserId(Guid Value) : StronglyTypedGuidId<UserId>(Value);