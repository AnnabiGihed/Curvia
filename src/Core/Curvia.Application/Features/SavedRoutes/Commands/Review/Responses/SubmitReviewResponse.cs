namespace Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;

/// <summary>Response returned after a review is submitted.</summary>
public sealed record SubmitReviewResponse(Guid SavedRouteId, int Rating, string? Comment, DateTime ReviewedAtUtc);