using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Application.Features.Awareness.Contracts.Records;

/// <summary>
/// Raw hazard record from a data provider.
/// </summary>
public sealed record HazardRecord(string ExternalId, double Latitude, double Longitude, string Source, HazardType HazardType, string? Description);