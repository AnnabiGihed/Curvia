using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

/// <summary>
/// Data transfer object representing a pending motorcycle model suggestion.
/// Includes maker name for moderation convenience.
/// </summary>
/// <param name="Id">Model identifier.</param>
/// <param name="MakerId">Owning maker identifier.</param>
/// <param name="MakerName">Resolved maker name (fallback may be "Unknown").</param>
/// <param name="Name">Suggested model name.</param>
/// <param name="Category">Riding category classification.</param>
/// <param name="SuggestedByUserId">User who suggested the model (null if system/seeded).</param>
/// <param name="CreatedOnUtc">UTC timestamp when the suggestion was created.</param>
public sealed record PendingModelDto(Guid Id, Guid MakerId, string MakerName, string Name, MotorcycleCategory Category, Guid? SuggestedByUserId, DateTime CreatedOnUtc);