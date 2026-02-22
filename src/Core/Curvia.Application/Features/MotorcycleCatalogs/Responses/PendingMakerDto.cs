namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

/// <summary>
/// Data transfer object representing a pending motorcycle maker suggestion.
/// Used by admin moderation screens.
/// </summary>
/// <param name="Id">Maker identifier.</param>
/// <param name="Name">Suggested maker name.</param>
/// <param name="SuggestedByUserId">User who suggested the maker (null if system/seeded).</param>
/// <param name="CreatedOnUtc">UTC timestamp when the suggestion was created.</param>
public sealed record PendingMakerDto(Guid Id, string Name, Guid? SuggestedByUserId, DateTime CreatedOnUtc);