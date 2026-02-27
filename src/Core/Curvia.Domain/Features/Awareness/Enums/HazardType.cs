namespace Curvia.Domain.Features.Awareness.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Classification of a permanent road hazard relevant to motorcyclists.
///              Sourced from OSM tags (hazard=*, ford=yes, railway=level_crossing, etc.).
///              Other covers any tagged hazard type not explicitly modelled here.
/// </summary>
public enum HazardType
{
	Other = 0,
	SlipperyRoad = 1,
	RoadDamage = 2,
	NarrowRoad = 3,
	SteepDescent = 4,
	SharpBend = 5,
	LevelCrossing = 6,
	Ford = 7,
	CattleGrid = 8,
	FloodingProne = 9,
	FallingRocks = 10
}