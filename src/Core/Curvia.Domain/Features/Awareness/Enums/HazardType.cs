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
	/// <summary>A hazard type that does not match any explicitly modelled category.</summary>
	Other = 0,

	/// <summary>Road surface with reduced grip — ice, wet leaves, oil, or polished tarmac. OSM: hazard=slippery_road.</summary>
	SlipperyRoad = 1,

	/// <summary>Deteriorated road surface — potholes, cracks, loose gravel, or broken asphalt. OSM: hazard=road_damage.</summary>
	RoadDamage = 2,

	/// <summary>Lane width insufficient for safe overtaking or lane-keeping at normal speed. OSM: hazard=narrow_road or width restrictions.</summary>
	NarrowRoad = 3,

	/// <summary>Steep downhill gradient that significantly increases braking distance. OSM: hazard=steep_descent.</summary>
	SteepDescent = 4,

	/// <summary>Tight or blind bend requiring a significant speed reduction. OSM: hazard=sharp_bend.</summary>
	SharpBend = 5,

	/// <summary>At-grade railway crossing where the road intersects an active rail line. OSM: railway=level_crossing.</summary>
	LevelCrossing = 6,

	/// <summary>Road crossing through open water — river, stream, or flood-prone section with no bridge. Depth and surface are unpredictable. OSM: ford=yes.</summary>
	Ford = 7,

	/// <summary>Metal grid across the road designed to prevent livestock from crossing. Can cause loss of grip, especially when wet. OSM: barrier=cattle_grid.</summary>
	CattleGrid = 8,

	/// <summary>Location known to flood during or after heavy rainfall, temporarily making the road impassable. OSM: hazard=flooding.</summary>
	FloodingProne = 9,

	/// <summary>Area prone to rockfall or landslide debris onto the road surface. OSM: hazard=falling_rocks or natural=cliff proximity.</summary>
	FallingRocks = 10
}