namespace Curvia.Domain.Features.Awareness.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Discriminator for the type of awareness item returned to the client.
///              Used as a filter parameter in awareness queries so mobile clients can
///              request only the categories they need (e.g. cameras only for a "camera
///              overlay" toggle).
/// </summary>
public enum AwarenessItemType
{
	SpeedCamera = 1,
	Hazard = 2,
	RoadWork = 3,
	Incident = 4
}