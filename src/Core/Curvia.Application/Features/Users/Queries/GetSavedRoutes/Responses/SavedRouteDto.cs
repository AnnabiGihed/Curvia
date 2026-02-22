using Curvia.Domain.Features.SavedRoutes.Enums;

namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

/// <summary>
/// Flat DTO for a saved route with its full review collection.
/// All domain value objects are unwrapped to primitives.
///
/// REVIEWS:
///   Replaced the single nullable ReviewDto? with a collection — a route can now
///   receive community reviews from multiple riders when Visibility = Public.
///   An empty list means no reviews yet.
/// </summary>
public sealed record SavedRouteDto(
	Guid SavedRouteId,
	Guid RouteId,
	string Name,
	string? Notes,
	SavedRouteVisibility Visibility,
	DateTime SavedAtUtc,
	IReadOnlyList<ReviewDto> Reviews);