using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

/// <summary>
/// Data transfer object representing a motorcycle model within a maker’s catalog.
/// </summary>
/// <param name="Id">Model identifier.</param>
/// <param name="MakerId">Owning maker identifier.</param>
/// <param name="Name">Model display name.</param>
/// <param name="Category">Riding category classification.</param>
public sealed record ModelDto(Guid Id, Guid MakerId, string Name, MotorcycleCategory Category);