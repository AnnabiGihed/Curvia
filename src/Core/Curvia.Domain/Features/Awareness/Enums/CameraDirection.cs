namespace Curvia.Domain.Features.Awareness.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Direction in which a speed camera enforces the speed limit.
///              Sourced from OSM "direction" tag and OpenSpeedCam dataset.
///              Unknown is used when the source data does not specify direction.
/// </summary>
public enum CameraDirection
{
	Unknown = 0,
	Both = 1,
	Forward = 2,
	Backward = 3
}