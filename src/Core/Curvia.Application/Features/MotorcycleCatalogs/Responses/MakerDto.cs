namespace Curvia.Application.Features.MotorcycleCatalogs.Responses;

/// <summary>
/// Data transfer object representing a motorcycle maker in the catalog.
/// </summary>
/// <param name="Id">Maker identifier.</param>
/// <param name="Name">Maker display name.</param>
public sealed record MakerDto(Guid Id, string Name);