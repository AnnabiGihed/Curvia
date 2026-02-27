using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Application.Features.Awareness.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Flat DTO for a live incident returned to API consumers.
/// </summary>
public sealed record IncidentDto(Guid Id, double Latitude, double Longitude, IncidentType IncidentType, IncidentSeverity Severity, string? Description, DateTime ValidFromUtc, DateTime ValidUntilUtc, string Source, string CountryCode);