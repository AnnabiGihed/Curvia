using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;

/// <summary>
/// Response returned after a route is saved.
/// All values are primitives — DTOs never expose domain value objects.
/// </summary>
public sealed record SaveRouteResponse(
	Guid SavedRouteId,
	Guid RouteId,
	string Name,
	SavedRouteVisibility Visibility,
	DateTime CreatedAtUtc);