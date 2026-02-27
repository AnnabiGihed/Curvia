using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Application.Features.Awareness.Contracts.Records;

/// <summary>
/// Raw speed camera record from a data provider.
/// </summary>
public sealed record SpeedCameraRecord(string ExternalId, double Latitude, double Longitude, int? SpeedLimitKmh, CameraDirection Direction);
