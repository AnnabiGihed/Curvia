using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.API.DTOs.MotorcycleCatalogs;

public sealed record AddModelRequest(string Name, MotorcycleCategory Category);