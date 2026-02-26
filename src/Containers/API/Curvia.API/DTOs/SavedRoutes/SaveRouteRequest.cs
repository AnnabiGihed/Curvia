using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.API.DTOs.SavedRoutes;

/// <summary>
/// Request body for saving a generated route.
/// </summary>
public sealed record SaveRouteRequest(Guid RouteId, string Name, string? Notes = null, SavedRouteVisibility Visibility = SavedRouteVisibility.Private);