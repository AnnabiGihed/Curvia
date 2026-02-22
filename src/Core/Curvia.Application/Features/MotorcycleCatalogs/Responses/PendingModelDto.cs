using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

public sealed record PendingModelDto(Guid Id, Guid MakerId, string MakerName, string Name, MotorcycleCategory Category, Guid? SuggestedByUserId, DateTime CreatedOnUtc);
