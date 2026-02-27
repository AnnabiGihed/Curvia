namespace Curvia.Application.Features.Awareness.Contracts.Records;

/// <summary>
/// Raw road work record from a data provider.
/// </summary>
public sealed record RoadWorkRecord(string ExternalId, double Latitude, double Longitude, string Title, string? Description, DateTime ValidFromUtc, DateTime ValidUntilUtc);