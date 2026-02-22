using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

/// <summary>
/// Flat DTO for a saved route with its full review collection.
/// All domain value objects are unwrapped to primitives.
/// </summary>
public sealed record SavedRouteDto(Guid SavedRouteId, Guid RouteId,	string Name, string? Notes,	SavedRouteVisibility Visibility, DateTime SavedAtUtc, IReadOnlyList<ReviewDto> Reviews);