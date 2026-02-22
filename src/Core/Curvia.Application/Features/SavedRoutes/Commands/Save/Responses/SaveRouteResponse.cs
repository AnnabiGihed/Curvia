using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;

/// <summary>
/// Response returned after a route is saved.
/// All values are primitives — DTOs never expose domain value objects.
/// </summary>
/// <param name="SavedRouteId">Identifier of the newly created SavedRoute aggregate.</param>
/// <param name="RouteId">Identifier of the referenced generated route.</param>
/// <param name="Name">User-given saved route name.</param>
/// <param name="Visibility">Visibility applied to the saved route.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the saved route was created.</param>
public sealed record SaveRouteResponse(Guid SavedRouteId, Guid RouteId, string Name, SavedRouteVisibility Visibility, DateTime CreatedAtUtc);