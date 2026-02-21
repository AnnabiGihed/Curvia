using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;

/// <summary>Response returned after a route is saved.</summary>
public sealed record SaveRouteResponse(Guid SavedRouteId, Guid RouteId,	string Name, SavedRouteVisibility Visibility, DateTime CreatedAtUtc);