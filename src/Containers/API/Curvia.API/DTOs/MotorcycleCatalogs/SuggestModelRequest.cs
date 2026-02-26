using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.API.DTOs.MotorcycleCatalogs;

public sealed record SuggestModelRequest(string Name, MotorcycleCategory Category);