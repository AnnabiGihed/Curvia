using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Application.Features.Awareness.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Flat DTO for a permanent road hazard returned to API consumers.
/// </summary>
public sealed record HazardDto(Guid Id, double Latitude, double Longitude, HazardType HazardType, string? Description, string Source, string CountryCode);
