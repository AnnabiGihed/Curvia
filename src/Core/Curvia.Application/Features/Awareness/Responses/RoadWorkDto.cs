namespace Curvia.Application.Features.Awareness.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Flat DTO for a road work returned to API consumers.
/// </summary>
public sealed record RoadWorkDto(Guid Id, double Latitude, double Longitude, string Title, string? Description, DateTime ValidFromUtc, DateTime ValidUntilUtc, string Source, string CountryCode);