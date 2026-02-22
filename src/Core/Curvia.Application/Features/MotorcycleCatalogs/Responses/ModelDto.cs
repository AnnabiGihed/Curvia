using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

public sealed record ModelDto(Guid Id, Guid MakerId,string Name, MotorcycleCategory Category);