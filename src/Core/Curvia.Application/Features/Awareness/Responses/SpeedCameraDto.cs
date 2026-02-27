using Curvia.Domain.Features.Awareness.Enums;

namespace Curvia.Application.Features.Awareness.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Flat DTO for a speed camera returned to API consumers.
/// </summary>
public sealed record SpeedCameraDto(Guid Id, double Latitude, double Longitude, int? SpeedLimitKmh, CameraDirection Direction, string Source, string CountryCode);