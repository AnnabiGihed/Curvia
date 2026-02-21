using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

/// <summary>Flat DTO for a saved route with its optional review.</summary>
public sealed record SavedRouteDto(Guid SavedRouteId, Guid RouteId,	string Name, string? Notes, SavedRouteVisibility Visibility, DateTime SavedAtUtc, ReviewDto? Review);