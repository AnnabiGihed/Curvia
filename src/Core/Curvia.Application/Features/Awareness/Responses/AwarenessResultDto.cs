namespace Curvia.Application.Features.Awareness.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Composite response returned by both awareness query endpoints.
///
///              DataFreshnessUtc reflects the oldest LastSyncedUtc across all returned items —
///              the client uses this to decide whether to show a "data may be outdated" warning.
///
///              Only the categories requested via the Types filter are populated;
///              unrequested lists are empty, never null.
/// </summary>
public sealed record AwarenessResultDto(IReadOnlyList<SpeedCameraDto> SpeedCameras, IReadOnlyList<HazardDto> Hazards, IReadOnlyList<RoadWorkDto> RoadWorks, IReadOnlyList<IncidentDto> Incidents, DateTime DataFreshnessUtc);
