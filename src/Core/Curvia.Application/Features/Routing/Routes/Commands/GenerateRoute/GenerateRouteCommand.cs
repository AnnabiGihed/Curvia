using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command to generate the best fun motorcycle route based on the rider's preferences.
///              Supports point-to-point trips, same-road roundtrips and different-road roundtrips.
///              The <see cref="Preset"/> field selects a pre-tuned scoring profile for one-tap UX;
///              set it to <see cref="RoutePreset.Custom"/> to use the individual weight fields instead.
/// </summary>
public sealed class GenerateRouteCommand : ICommand<GenerateRouteResponse>
{
	#region Origin
	/// <summary>Start point latitude in decimal degrees (WGS84). Required.</summary>
	public double StartLatitude { get; init; }

	/// <summary>Start point longitude in decimal degrees (WGS84). Required.</summary>
	public double StartLongitude { get; init; }
	#endregion

	#region Constraints
	/// <summary>
	/// Optional maximum riding time in minutes.
	/// Candidates exceeding this duration are disqualified.
	/// Null = unlimited.
	/// </summary>
	public int? MaxDurationMinutes { get; init; }

	/// <summary>Avoid toll roads. Default: false.</summary>
	public bool AvoidTolls { get; init; } = false;

	/// <summary>
	/// Optional maximum total distance in meters.
	/// Null = unlimited.
	/// </summary>
	public double? MaxDistanceMeters { get; init; }

	/// <summary>Avoid motorways and dual carriageways. Default: false.</summary>
	public bool AvoidHighways { get; init; } = false;

	/// <summary>
	/// Avoid unpaved roads, gravel tracks and off-road trails. Default: false.
	/// Set to true for road motorcycles not suitable for off-road surfaces.
	/// </summary>
	public bool AvoidUnpaved { get; init; } = false;

	/// <summary>
	/// Maximum allowed route distance as a ratio over the baseline distance.
	/// Example: 2.0 allows a route up to twice the direct distance.
	/// Range: [1.0 – 10.0]. Default: 3.0.
	/// </summary>
	public double MaxDetourRatio { get; init; } = 3.0;

	/// <summary>
	/// Controls how aggressively urban streets and town centres are avoided.
	/// Default: <see cref="UrbanTolerance.Allowed"/>.
	/// </summary>
	public UrbanTolerance UrbanTolerance { get; init; } = UrbanTolerance.Allowed;
	#endregion

	#region Optional Waypoints
	/// <summary>
	/// Ordered intermediate waypoints the route must pass through.
	/// Maximum 25 waypoints.
	/// </summary>
	public IReadOnlyList<WaypointDto>? Waypoints { get; init; }
	#endregion

	#region Ride Personality — Preset or Custom weights
	/// <summary>
	/// Custom only: global fun multiplier [0–10].
	/// Higher values produce more aggressive detours in favour of enjoyable roads.
	/// Required when <see cref="Preset"/> is <see cref="RoutePreset.Custom"/>.
	/// </summary>
	public double? FunFactor { get; init; }

	/// <summary>Custom only: weight assigned to road curvature/twistiness [0–1].</summary>
	public double? CurvesWeight { get; init; }

	/// <summary>Custom only: weight assigned to scenic surroundings [0–1].</summary>
	public double? SceneryWeight { get; init; }

	/// <summary>Custom only: weight assigned to elevation changes and mountain relief [0–1].</summary>
	public double? ElevationWeight { get; init; }

	/// <summary>
	/// Selects a pre-tuned scoring profile.
	/// Default: <see cref="RoutePreset.Balanced"/>.
	/// Set to <see cref="RoutePreset.Custom"/> to use the individual weight properties below.
	/// </summary>
	public RoutePreset Preset { get; init; } = RoutePreset.Balanced;
	#endregion

	#region Trip Mode — exactly one of the following groups must be set
	/// <summary>
	/// Point-to-point mode: destination latitude.
	/// Set together with <see cref="EndLongitude"/>.
	/// Mutually exclusive with <see cref="LoopTargetDistanceMeters"/>.
	/// </summary>
	public double? EndLatitude { get; init; }

	/// <summary>
	/// Point-to-point mode: destination longitude.
	/// Set together with <see cref="EndLatitude"/>.
	/// Mutually exclusive with <see cref="LoopTargetDistanceMeters"/>.
	/// </summary>
	public double? EndLongitude { get; init; }

	/// <summary>
	/// Loop mode: desired total loop distance in meters.
	/// The route will start and end at the same location.
	/// Mutually exclusive with <see cref="EndLatitude"/> / <see cref="EndLongitude"/>.
	/// Range: [1,000 – 500,000] m.
	/// </summary>
	public double? LoopTargetDistanceMeters { get; init; }

	/// <summary>
	/// Loop mode only: controls how the return leg is generated.
	/// <see cref="ReturnStrategy.SameRoute"/> — single circular loop (default).
	/// <see cref="ReturnStrategy.DifferentRoute"/> — outbound + return on different roads.
	/// Ignored when in point-to-point mode.
	/// </summary>
	public ReturnStrategy LoopReturnStrategy { get; init; } = ReturnStrategy.SameRoute;
	#endregion
}