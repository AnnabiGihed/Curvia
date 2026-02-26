using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.API.DTOs.SavedRoutes;

/// <summary>
/// Request body for updating a saved route's metadata.
/// </summary>
public sealed record UpdateSavedRouteRequest(string Name, string? Notes, SavedRouteVisibility Visibility);