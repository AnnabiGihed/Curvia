namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

/// <summary>Flat DTO for an embedded review.</summary>
public sealed record ReviewDto(Guid id, int Rating, string? Comment, DateTime ReviewedAtUtc);
