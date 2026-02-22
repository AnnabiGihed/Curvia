namespace Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;

/// <summary>
/// Response returned after a review is submitted or updated.
/// All values are primitives — DTOs never expose domain value objects.
/// </summary>
public sealed record SubmitReviewResponse(
	Guid SavedRouteId,
	Guid ReviewId,
	Guid ReviewerUserId,
	int Rating,
	string? Comment,
	DateTime ReviewedAtUtc);