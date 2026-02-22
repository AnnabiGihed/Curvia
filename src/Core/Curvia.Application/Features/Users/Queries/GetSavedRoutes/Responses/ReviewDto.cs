namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

/// <summary>
/// Flat DTO for a single review in a saved route's review collection.
/// All domain value objects are unwrapped to primitives — DTOs never expose domain types.
/// </summary>
public sealed record ReviewDto(Guid ReviewId, Guid ReviewerUserId, int Rating, string? Comment, DateTime ReviewedAtUtc);