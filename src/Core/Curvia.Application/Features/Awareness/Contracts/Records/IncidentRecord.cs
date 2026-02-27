using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Application.Features.Awareness.Contracts.Records;

/// <summary>Raw incident record from a DATEX II or equivalent feed.</summary>
public sealed record IncidentRecord(string ExternalId, double Latitude, double Longitude, IncidentType IncidentType, IncidentSeverity Severity,	string? Description, DateTime ValidFromUtc,	DateTime ValidUntilUtc);