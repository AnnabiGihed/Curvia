using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routes.Commands.Generate.Responses;
using Curvia.Domain.Features.Routing.RoutePlans.Enums;

namespace Curvia.Application.Features.Routes.Commands.Generate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command to generate the best fun motorcycle route.
/// </summary>
public sealed class GenerateRouteCommand : ICommand<GenerateRouteResponse>
{
	#region Origin
	/// <summary>Start coordinate latitude (WGS84). Required.</summary>
	public double StartLatitude { get; init; }

	/// <summary>Start coordinate longitude (WGS84). Required.</summary>
	public double StartLongitude { get; init; }
	#endregion

	#region Routing constraints
	/// <summary>When true, toll roads are strongly avoided.</summary>
	public bool AvoidTolls { get; init; }

	/// <summary>When true, unpaved roads and gravel tracks are avoided.</summary>
	public bool AvoidUnpaved { get; init; }

	/// <summary>When true, motorways and dual carriageways are strongly avoided.</summary>
	public bool AvoidHighways { get; init; }

	/// <summary>Maximum total ride duration in minutes. Null = no cap. Range: [1–1,440].</summary>
	public int? MaxDurationMinutes { get; init; }
	
	/// <summary>Maximum total distance in meters. Null = no cap.</summary>
	public double? MaxDistanceMeters { get; init; }

	/// <summary>
	/// Maximum allowed route distance as a multiple of the straight-line baseline.
	/// Default: 2.0.
	/// </summary>
	public double MaxDetourRatio { get; init; } = 2.0;

	/// <summary>Controls how aggressively urban streets are avoided. Default: Allowed.</summary>
	public UrbanTolerance UrbanTolerance { get; init; } = UrbanTolerance.Allowed;
	#endregion

	#region Optional Waypoints
	/// <summary>Ordered intermediate waypoints the route must pass through. Maximum 25.</summary>
	public IReadOnlyList<WaypointDto>? Waypoints { get; init; }
	#endregion

	#region Ride Personality
	/// <summary>Custom only: global fun multiplier [0–10]. Required when Preset = Custom.</summary>
	public double? FunFactor { get; init; }

	/// <summary>Custom only: weight for curvature/twistiness [0–1].</summary>
	public double? CurvesWeight { get; init; }

	/// <summary>Custom only: weight for scenic surroundings [0–1].</summary>
	public double? SceneryWeight { get; init; }

	/// <summary>Custom only: weight for elevation/mountain relief [0–1].</summary>
	public double? ElevationWeight { get; init; }

	/// <summary>Pre-tuned scoring profile. Default: Balanced.</summary>
	public RoutePreset Preset { get; init; } = RoutePreset.Balanced;
	#endregion

	#region Trip Mode
	/// <summary>
	/// Point-to-point only: when true, also generates a return leg (End → Start)
	/// using roads that are different from the outbound journey.
	/// The outbound corridor is excluded from the return search via Valhalla exclude_polygons.
	/// The response ReturnLeg field will be populated.
	/// Ignored in loop mode. Default: false.
	/// </summary>
	public bool RoundTrip { get; init; }

	/// <summary>
	/// Point-to-point destination latitude.
	/// Mutually exclusive with LoopTargetDistanceMeters.
	/// </summary>
	public double? EndLatitude { get; init; }

	/// <summary>
	/// Point-to-point destination longitude.
	/// Mutually exclusive with LoopTargetDistanceMeters.
	/// </summary>
	public double? EndLongitude { get; init; }

	/// <summary>
	/// Loop mode: desired total loop distance in meters [1,000–500,000].
	/// Mutually exclusive with EndLatitude/EndLongitude.
	/// </summary>
	public double? LoopTargetDistanceMeters { get; init; }

	/// <summary>
	/// Loop mode only: SameRoute (0) or DifferentRoute (1).
	/// Ignored in point-to-point mode.
	/// </summary>
	public ReturnStrategy LoopReturnStrategy { get; init; } = ReturnStrategy.SameRoute;
	#endregion
}