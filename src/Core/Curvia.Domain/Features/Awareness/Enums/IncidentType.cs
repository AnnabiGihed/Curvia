namespace Curvia.Domain.Features.Awareness.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Classification of a live traffic incident sourced from DATEX II feeds.
///              Maps to the DATEX II SituationRecord subtypes.
/// </summary>
public enum IncidentType
{
	Other = 0,
	Accident = 1,
	RoadClosure = 2,
	Obstruction = 3,
	WeatherHazard = 4,
	WorkInProgress = 5,
	AbnormalLoad = 6
}