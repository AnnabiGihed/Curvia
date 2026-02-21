using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.CandidateGenerator;
using Curvia.Infrastructure.Features.Routing.Routes.Helpers;

namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.CandidateGenerator;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Generates Valhalla route candidates for a given RoutePlan.
///
/// ═══════════════════════════════════════════════════════════════════════
/// CORE DESIGN: LATERAL WAYPOINTS FOR ALL POINT-TO-POINT ROUTES
/// ═══════════════════════════════════════════════════════════════════════
///
/// Valhalla motorcycle costing has no use_hills parameter (bicycle-only).
/// Sending it is silently ignored — zero effect on routing.
/// Without something to force Valhalla off the direct corridor, all three
/// costing variants converge on the same flat N-road path.
///
/// Solution: every variant gets a DIFFERENT lateral "through" waypoint placed
/// off the direct axis. Valhalla MUST route through that point, which forces
/// it through geographically different terrain (valleys, ridges, back roads).
///
/// ═══════════════════════════════════════════════════════════════════════
/// OUTBOUND (Start → End) — GeneratePointToPointVariantsAsync
/// ═══════════════════════════════════════════════════════════════════════
///
///   left-early  — waypoint at 40% along axis, LEFT,  25% lateral offset
///   right-mid   — waypoint at 55% along axis, RIGHT, 20% lateral offset
///   left-late   — waypoint at 65% along axis, LEFT,  15% lateral offset
///
/// ═══════════════════════════════════════════════════════════════════════
/// RETURN LEG (End → Start) — GenerateRoundTripReturnVariantsAsync
/// ═══════════════════════════════════════════════════════════════════════
///
/// The return leg previously used a single perpendicular midpoint waypoint,
/// producing identical results every call (same waypoint = same road).
///
/// Fixed: same lateral waypoint strategy as the outbound, applied to the
/// reversed axis (End → Start). Different positions and sides than the
/// outbound, so the return uses genuinely different corridors:
///
///   return-right-early — waypoint at 35% of End→Start, RIGHT, 22% offset
///   return-left-mid    — waypoint at 50% of End→Start, LEFT,  18% offset
///   return-right-late  — waypoint at 60% of End→Start, RIGHT, 14% offset
///
/// The positions and sides are intentionally different from the outbound
/// (outbound uses left-40%, right-55%, left-65%) to maximise road variety.
///
/// ═══════════════════════════════════════════════════════════════════════
/// LOOPS — Direct intermediate offset
/// ═══════════════════════════════════════════════════════════════════════
///
/// For loops: Start → east-offset intermediate → Start.
/// DifferentRoute: south-offset + bbox exclusion for the return.
///
/// ═══════════════════════════════════════════════════════════════════════
/// LATERAL OFFSET CLAMPING
/// ═══════════════════════════════════════════════════════════════════════
///
///   Min: 12 km — ensures waypoint is far enough off the main road
///   Max: 45 km — prevents waypoint landing in inaccessible terrain
/// </summary>
internal sealed class RouteCandidateGeneratorService : IRouteCandidateGeneratorService
{
	#region Constants
	private readonly IValhallaRoutingClient _client;
	private const double EarthRadiusMeters = 6_371_000.0;

	// Lateral offset for outbound and return leg lateral waypoints
	private const double LateralOffsetFraction = 0.25;
	private const double MinLateralOffsetMeters = 12_000;
	private const double MaxLateralOffsetMeters = 45_000;
	#endregion

	#region Constructor
	public RouteCandidateGeneratorService(IValhallaRoutingClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}
	#endregion

	// ═══════════════════════════════════════════════════════════════
	// IRouteCandidateGeneratorService — Outbound
	// ═══════════════════════════════════════════════════════════════

	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateAsync(
		RoutePlan plan,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);

		// Point-to-point without explicit user waypoints → lateral waypoint strategy
		if (plan.End is not null && plan.Waypoints.Count == 0)
			return await GeneratePointToPointVariantsAsync(plan, plan.Start, plan.End, isReturn: false, cancellationToken);

		// Loops and explicit-waypoint routes → costing-only variants, direct path
		var variants = BuildCostingVariants(plan);
		var locations = BuildDirectLocations(plan, intermediateOffsetBearing: null);
		return await ExecuteVariantsAsync(locations, variants, excludePolygons: null, cancellationToken);
	}

	// ═══════════════════════════════════════════════════════════════
	// IRouteCandidateGeneratorService — DifferentRoute loop return
	// ═══════════════════════════════════════════════════════════════

	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateReturnAsync(
		RoutePlan plan,
		ValhallaRouteResponse outboundResponse,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);
		ArgumentNullException.ThrowIfNull(outboundResponse);

		var variants = BuildCostingVariants(plan);
		var locations = BuildDirectLocations(plan, intermediateOffsetBearing: 180.0);
		var excludePolygons = BuildExcludePolygons(outboundResponse);
		return await ExecuteVariantsAsync(locations, variants, excludePolygons, cancellationToken);
	}

	// ═══════════════════════════════════════════════════════════════
	// IRouteCandidateGeneratorService — Round-trip return leg
	// ═══════════════════════════════════════════════════════════════

	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateRoundTripReturnAsync(
		RoutePlan plan,
		ValhallaRouteResponse outboundResponse,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);
		ArgumentNullException.ThrowIfNull(outboundResponse);

		if (plan.End is null)
			throw new InvalidOperationException(
				"GenerateRoundTripReturnAsync requires a point-to-point plan with a non-null End.");

		// Return leg direction is End → Start (reversed axis).
		// Uses the same lateral waypoint strategy as the outbound but with
		// different positions and sides to maximise road variety.
		return await GeneratePointToPointVariantsAsync(
			plan,
			origin: plan.End,
			destination: plan.Start,
			isReturn: true,
			cancellationToken);
	}

	// ═══════════════════════════════════════════════════════════════
	// PRIVATE — Lateral waypoint variant generation (outbound + return)
	// ═══════════════════════════════════════════════════════════════

	/// <summary>
	/// Generates 3 candidates, each routed through a different lateral waypoint.
	///
	/// For the outbound (isReturn = false):
	///   left-early  — 40% along axis, LEFT,  25% offset
	///   right-mid   — 55% along axis, RIGHT, 20% offset
	///   left-late   — 65% along axis, LEFT,  15% offset
	///
	/// For the return leg (isReturn = true):
	///   return-right-early — 35% along End→Start axis, RIGHT, 22% offset
	///   return-left-mid    — 50% along End→Start axis, LEFT,  18% offset
	///   return-right-late  — 60% along End→Start axis, RIGHT, 14% offset
	///
	/// Positions and sides differ from the outbound intentionally — if the outbound
	/// used the Meuse-west corridor (left of Brussels→Houffalize axis), the return
	/// uses the right side, pulling it toward the Ourthe/Liège-side valleys.
	/// </summary>
	private async Task<IReadOnlyList<RouteCandidate>> GeneratePointToPointVariantsAsync(
		RoutePlan plan,
		GeoCoordinate origin,
		GeoCoordinate destination,
		bool isReturn,
		CancellationToken cancellationToken)
	{
		var straightMeters = HaversineMeters(origin, destination);
		var lateralBase = Math.Clamp(
			straightMeters * LateralOffsetFraction,
			MinLateralOffsetMeters,
			MaxLateralOffsetMeters);

		var forwardBearing = Bearing(origin, destination);
		var leftBearing = (forwardBearing - 90.0 + 360.0) % 360.0;
		var rightBearing = (forwardBearing + 90.0) % 360.0;

		// Outbound and return variants use different positions + sides so that together
		// they explore genuinely different parts of the terrain between the two cities.
		var variants = isReturn
			? new[]
			{
				// Return: right-early, left-mid, right-late
				// (opposite sides from outbound left-early, right-mid, left-late)
				(
					Name:     "return-right-early",
					Waypoint: LateralWaypoint(origin, destination, progress: 0.35, bearing: rightBearing, meters: lateralBase * 0.88),
					Costing:  MakeCosting(plan, useHighways: 0.05, useTolls: 0.3)
				),
				(
					Name:     "return-left-mid",
					Waypoint: LateralWaypoint(origin, destination, progress: 0.50, bearing: leftBearing,  meters: lateralBase * 0.72),
					Costing:  MakeCosting(plan, useHighways: 0.2,  useTolls: 0.5)
				),
				(
					Name:     "return-right-late",
					Waypoint: LateralWaypoint(origin, destination, progress: 0.60, bearing: rightBearing, meters: lateralBase * 0.56),
					Costing:  MakeCosting(plan, useHighways: 0.15, useTolls: 0.0, allowSlightUnpaved: true)
				)
			}
			: new[]
			{
				// Outbound: left-early, right-mid, left-late
				(
					Name:     "left-early",
					Waypoint: LateralWaypoint(origin, destination, progress: 0.40, bearing: leftBearing,  meters: lateralBase),
					Costing:  MakeCosting(plan, useHighways: 0.05, useTolls: 0.3)
				),
				(
					Name:     "right-mid",
					Waypoint: LateralWaypoint(origin, destination, progress: 0.55, bearing: rightBearing, meters: lateralBase * 0.80),
					Costing:  MakeCosting(plan, useHighways: 0.2,  useTolls: 0.5)
				),
				(
					Name:     "left-late",
					Waypoint: LateralWaypoint(origin, destination, progress: 0.65, bearing: leftBearing,  meters: lateralBase * 0.60),
					Costing:  MakeCosting(plan, useHighways: 0.15, useTolls: 0.0, allowSlightUnpaved: true)
				)
			};

		var results = new List<RouteCandidate>(variants.Length);

		foreach (var (name, waypoint, costing) in variants)
		{
			var locations = new List<ValhallaLocation>
			{
				new(origin.Latitude,      origin.Longitude,      "break"),
				new(waypoint.Lat,         waypoint.Lon,           "through"),
				new(destination.Latitude, destination.Longitude, "break")
			};

			try
			{
				var req = new ValhallaRouteRequest(
					Locations: locations,
					Costing: "motorcycle",
					CostingOptions: new ValhallaCostingOptions(costing),
					DirectionsOptions: new ValhallaDirectionsOptions(),
					ExcludePolygons: null);

				var raw = await _client.RouteAsync(req, cancellationToken);
				results.Add(new RouteCandidate(raw, name));
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
			{
				// Waypoint inaccessible or Valhalla timeout — skip this variant.
			}
		}

		return results;
	}

	/// <summary>
	/// Computes a lateral intermediate waypoint at <paramref name="progress"/> fraction
	/// along the origin→destination axis, then offset laterally by <paramref name="meters"/>.
	/// </summary>
	private static (double Lat, double Lon) LateralWaypoint(
		GeoCoordinate origin,
		GeoCoordinate destination,
		double progress,
		double bearing,
		double meters)
	{
		var axisLat = origin.Latitude + progress * (destination.Latitude - origin.Latitude);
		var axisLon = origin.Longitude + progress * (destination.Longitude - origin.Longitude);
		var axisPoint = GeoCoordinate.Create(axisLat, axisLon).Value;
		return ComputeOffsetCoordinate(axisPoint, bearing, meters);
	}

	// ═══════════════════════════════════════════════════════════════
	// PRIVATE — Costing helpers
	// ═══════════════════════════════════════════════════════════════

	private static ValhallaMotorcycleOptions MakeCosting(
		RoutePlan plan,
		double useHighways,
		double useTolls,
		bool allowSlightUnpaved = false)
	{
		var c = plan.Constraints;

		var livingStreets = c.UrbanTolerance switch
		{
			UrbanTolerance.None => 0.05,
			UrbanTolerance.PassThrough => 0.2,
			UrbanTolerance.Allowed => 0.6,
			_ => 0.6
		};

		var useTrails = c.AvoidUnpaved ? 0.0 : (allowSlightUnpaved ? 0.3 : 0.5);

		if (c.AvoidHighways) useHighways = 0.0;
		if (c.AvoidTolls) useTolls = 0.0;

		return new ValhallaMotorcycleOptions(
			UseHighways: useHighways,
			UseTolls: useTolls,
			UseTrails: useTrails,
			UseLivingStreets: livingStreets);
	}

	/// <summary>
	/// Costing-only variants used for loops and routes with explicit user waypoints.
	/// Lateral waypoint variants are used for all point-to-point routes.
	/// </summary>
	private static (string Name, ValhallaMotorcycleOptions Moto)[] BuildCostingVariants(RoutePlan plan) =>
		new[]
		{
			("balanced", MakeCosting(plan, useHighways: 0.7,  useTolls: 0.9)),
			("scenic",   MakeCosting(plan, useHighways: 0.1,  useTolls: 0.5)),
			("explorer", MakeCosting(plan, useHighways: 0.2,  useTolls: 0.0, allowSlightUnpaved: true))
		};

	// ═══════════════════════════════════════════════════════════════
	// PRIVATE — Location building (loops / explicit waypoints)
	// ═══════════════════════════════════════════════════════════════

	private static IReadOnlyList<ValhallaLocation> BuildDirectLocations(
		RoutePlan plan,
		double? intermediateOffsetBearing)
	{
		var list = new List<ValhallaLocation>();
		list.Add(new ValhallaLocation(plan.Start.Latitude, plan.Start.Longitude, "break"));

		if (plan.LoopSpec is not null)
		{
			var bearing = intermediateOffsetBearing ?? 90.0;
			var offset = ComputeOffsetCoordinate(
				plan.Start,
				bearingDegrees: bearing,
				distanceMeters: plan.LoopSpec.TargetDistance.Meters / (2 * Math.PI));

			list.Add(new ValhallaLocation(offset.Lat, offset.Lon, "through"));
			list.Add(new ValhallaLocation(plan.Start.Latitude, plan.Start.Longitude, "break"));
		}
		else
		{
			foreach (var wp in plan.Waypoints)
				list.Add(new ValhallaLocation(wp.Location.Latitude, wp.Location.Longitude, "through"));

			if (plan.End is not null)
				list.Add(new ValhallaLocation(plan.End.Latitude, plan.End.Longitude, "break"));
		}

		return list;
	}

	// ═══════════════════════════════════════════════════════════════
	// PRIVATE — Execution
	// ═══════════════════════════════════════════════════════════════

	private async Task<IReadOnlyList<RouteCandidate>> ExecuteVariantsAsync(
		IReadOnlyList<ValhallaLocation> locations,
		(string Name, ValhallaMotorcycleOptions Moto)[] variants,
		IReadOnlyList<double[][]>? excludePolygons,
		CancellationToken cancellationToken)
	{
		var results = new List<RouteCandidate>(variants.Length);

		foreach (var (name, moto) in variants)
		{
			try
			{
				var req = new ValhallaRouteRequest(
					Locations: locations,
					Costing: "motorcycle",
					CostingOptions: new ValhallaCostingOptions(moto),
					DirectionsOptions: new ValhallaDirectionsOptions(),
					ExcludePolygons: excludePolygons);

				var raw = await _client.RouteAsync(req, cancellationToken);
				results.Add(new RouteCandidate(raw, name));
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
			{
				// No routable path or timeout — skip silently.
			}
		}

		return results;
	}

	// ═══════════════════════════════════════════════════════════════
	// PRIVATE — Exclusion polygon (DifferentRoute loops only)
	// ═══════════════════════════════════════════════════════════════

	private static IReadOnlyList<double[][]> BuildExcludePolygons(ValhallaRouteResponse outbound)
	{
		if (outbound.Trip?.Legs is null || outbound.Trip.Legs.Count == 0)
			return Array.Empty<double[][]>();

		var allPoints = outbound.Trip.Legs
			.Where(l => !string.IsNullOrWhiteSpace(l.Shape))
			.SelectMany(l => Polyline6Decoder.Decode(l.Shape))
			.ToList();

		if (allPoints.Count == 0)
			return Array.Empty<double[][]>();

		var minLat = allPoints.Min(p => p.Latitude);
		var maxLat = allPoints.Max(p => p.Latitude);
		var minLon = allPoints.Min(p => p.Longitude);
		var maxLon = allPoints.Max(p => p.Longitude);

		var latPad = (maxLat - minLat) * 0.10;
		var lonPad = (maxLon - minLon) * 0.10;
		minLat -= latPad; maxLat += latPad;
		minLon -= lonPad; maxLon += lonPad;

		return new List<double[][]>
		{
			new double[][]
			{
				new[] { minLon, minLat },
				new[] { maxLon, minLat },
				new[] { maxLon, maxLat },
				new[] { minLon, maxLat },
				new[] { minLon, minLat }
			}
		};
	}

	// ═══════════════════════════════════════════════════════════════
	// PRIVATE — Geodesy helpers
	// ═══════════════════════════════════════════════════════════════

	private static (double Lat, double Lon) ComputeOffsetCoordinate(
		GeoCoordinate origin, double bearingDegrees, double distanceMeters)
	{
		var lat1 = ToRad(origin.Latitude);
		var lon1 = ToRad(origin.Longitude);
		var bearing = ToRad(bearingDegrees);
		var d = distanceMeters / EarthRadiusMeters;

		var lat2 = Math.Asin(
			Math.Sin(lat1) * Math.Cos(d) +
			Math.Cos(lat1) * Math.Sin(d) * Math.Cos(bearing));

		var lon2 = lon1 + Math.Atan2(
			Math.Sin(bearing) * Math.Sin(d) * Math.Cos(lat1),
			Math.Cos(d) - Math.Sin(lat1) * Math.Sin(lat2));

		return (ToDeg(lat2), ToDeg(lon2));
	}

	private static double Bearing(GeoCoordinate a, GeoCoordinate b)
	{
		var lat1 = ToRad(a.Latitude);
		var lat2 = ToRad(b.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);
		var y = Math.Sin(dLon) * Math.Cos(lat2);
		var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
		return (ToDeg(Math.Atan2(y, x)) + 360.0) % 360.0;
	}

	private static double HaversineMeters(GeoCoordinate a, GeoCoordinate b)
	{
		var dLat = ToRad(b.Latitude - a.Latitude);
		var dLon = ToRad(b.Longitude - a.Longitude);
		var sinDLat = Math.Sin(dLat / 2);
		var sinDLon = Math.Sin(dLon / 2);
		var h = sinDLat * sinDLat +
				Math.Cos(ToRad(a.Latitude)) * Math.Cos(ToRad(b.Latitude)) * sinDLon * sinDLon;
		return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
	}

	private static double ToRad(double d) => d * Math.PI / 180.0;
	private static double ToDeg(double r) => r * 180.0 / Math.PI;
}