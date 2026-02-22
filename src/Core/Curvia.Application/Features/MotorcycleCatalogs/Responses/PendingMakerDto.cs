namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

public sealed record PendingMakerDto(Guid Id, string Name, Guid? SuggestedByUserId, DateTime CreatedOnUtc);