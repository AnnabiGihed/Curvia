namespace Curvia.API.DTOs.SavedRoutes;

/// <summary>
/// Request body for submitting a route review.
/// </summary>
public sealed record SubmitReviewRequest(int Rating, string? Comment = null);