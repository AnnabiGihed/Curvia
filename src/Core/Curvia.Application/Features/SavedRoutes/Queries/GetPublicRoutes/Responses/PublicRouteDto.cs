using Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

namespace Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes.Responses;

/// <summary>
/// Flat DTO for a public saved route in the community feed.
/// Notes are intentionally excluded — they are private to the owner.
/// All domain value objects are unwrapped to primitives.
/// </summary>
/// <param name="SavedRouteId">Identifier of the saved route.</param>
/// <param name="RouteId">Identifier of the underlying generated route.</param>
/// <param name="OwnerUserId">Application-level identifier of the user who saved the route.</param>
/// <param name="Name">User-given route name.</param>
/// <param name="SavedAtUtc">UTC timestamp when the route was saved.</param>
/// <param name="Reviews">Reviews submitted by community riders on this route.</param>
public sealed record PublicRouteDto(Guid SavedRouteId, Guid RouteId, Guid OwnerUserId, string Name, DateTime SavedAtUtc, IReadOnlyList<ReviewDto> Reviews);