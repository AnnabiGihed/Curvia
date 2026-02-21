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
///              Produces 3 named variants per call. Each differs in road-character preference
///              so the scoring service can evaluate genuinely distinct options:
///
///                twisty-hills   — use_hills=0.8. Prefers hilly terrain. Capped at 0.8
///                                 (not 1.0) to prevent Valhalla chasing dead-end hill roads.
///                                 At 1.0, Valhalla follows any gradient regardless of
///                                 connectivity, producing U-turns and backtrack artifacts.
///
///                scenic-valley  — use_hills=0.6. Moderate hill preference, allows minor
///                                 highways to connect valley sections (Meuse, Ourthe, Semois).
///
///                explorer       — use_hills=0.5. Balanced. Avoids tolls, slight unpaved
///                                 tolerance. Discovers secondary roads the other variants
///                                 miss without hill-obsession artifacts.
///
///              Perpendicular waypoint strategy for round-trip return legs:
///                A "through" waypoint is placed perpendicular to the midpoint of the
///                End→Start axis to pull the return route off the outbound corridor.
///                PerpendicularOffsetRatio reduced from 30% → 20% to avoid placing
///                the waypoint in geographically inaccessible terrain (hilltops, forests,
///                dead-end valleys) that cause Valhalla to produce U-turn artifacts.
///                Two opposite directions are tried; the scorer picks the better candidate.
/// </summary>
internal sealed class RouteCandidateGeneratorService : IRouteCandidateGeneratorService
{
	#region Fields
	private readonly IValhallaRoutingClient _client;
	private const double EarthRadiusMeters = 6_371_000.0;

	/// <summary>
	/// Perpendicular offset as a fraction of the straight-line journey distance.
	/// Reduced from 0.30 to 0.20 to keep waypoints closer to the main road network
	/// and avoid dead-end terrain that causes U-turn artifacts.
	/// </summary>
	private const double PerpendicularOffsetRatio = 0.20;

	/// <summary>Minimum perpendicular offset in meters.</summary>
	private const double MinPerpendicularOffsetMeters = 10_000;

	/// <summary>
	/// Maximum perpendicular offset in meters.
	/// Reduced from 80 km to 50 km — large offsets frequently land in forests or
	/// dead-end valleys, especially in hilly terrain like the Ardennes.
	/// </summary>
	private const double MaxPerpendicularOffsetMeters = 50_000;
	#endregion

	#region Constructor
	public RouteCandidateGeneratorService(IValhallaRoutingClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}
	#endregion

	#region IRouteCandidateGeneratorService — Outbound
	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateAsync(RoutePlan plan, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);
		var variants = BuildVariants(plan);
		var locations = BuildLocations(plan, intermediateOffsetBearing: null);
		return await ExecuteVariantsAsync(locations, variants, excludePolygons: null, cancellationToken);
	}
	#endregion

	#region IRouteCandidateGeneratorService — DifferentRoute loop return
	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateReturnAsync(
		RoutePlan plan,
		ValhallaRouteResponse outboundResponse,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);
		ArgumentNullException.ThrowIfNull(outboundResponse);

		var variants = BuildVariants(plan);
		var locations = BuildLocations(plan, intermediateOffsetBearing: 180.0);
		var excludePolygons = BuildExcludePolygons(outboundResponse);
		return await ExecuteVariantsAsync(locations, variants, excludePolygons, cancellationToken);
	}
	#endregion

	#region IRouteCandidateGeneratorService — Point-to-point round trip return
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
				"GenerateRoundTripReturnAsync requires a point-to-point plan with a non-null End coordinate.");

		var variants = BuildVariants(plan);

		// Try both perpendicular directions — scorer picks the better candidate.
		// Both run without exclusion polygons (exclusion bbox breaks point-to-point).
		var locationsLeft = BuildRoundTripReturnLocations(plan, perpendicularDirection: +1);
		var locationsRight = BuildRoundTripReturnLocations(plan, perpendicularDirection: -1);

		var leftTask = ExecuteVariantsAsync(locationsLeft, variants, excludePolygons: null, cancellationToken);
		var rightTask = ExecuteVariantsAsync(locationsRight, variants, excludePolygons: null, cancellationToken);

		var results = await Task.WhenAll(leftTask, rightTask);
		return results[0].Concat(results[1]).ToList();
	}
	#endregion

	#region Private — Variant definitions

	/// <summary>
	/// Builds 3 route variants differentiated by use_hills and highway tolerance.
	///
	/// use_hills tuning rationale:
	///   use_hills = 1.0 → Valhalla chases ANY gradient, including dead-end hill roads,
	///                      single-track forest tracks and culs-de-sac. Produces U-turn and
	///                      backtrack artifacts visible in the Cetturu and Hermeton sections.
	///   use_hills = 0.8 → Prefers hilly terrain but Valhalla still requires road connectivity.
	///                      Finds Meuse valley roads, Ardennes ridge routes without dead-ends.
	///   use_hills = 0.5 → Balanced. Routes through hilly areas when practical but does not
	///                      sacrifice connectivity. Best fallback candidate.
	///
	/// Hard rider constraints (avoidHighways, avoidTolls, avoidUnpaved, urbanTolerance)
	/// override variant defaults.
	/// </summary>
	private static (string Name, ValhallaMotorcycleOptions Moto)[] BuildVariants(RoutePlan plan)
	{
		var c = plan.Constraints;

		var livingStreets = c.UrbanTolerance switch
		{
			UrbanTolerance.None => 0.05,
			UrbanTolerance.PassThrough => 0.2,
			UrbanTolerance.Allowed => 0.6,
			_ => 0.6
		};

		var serviceFactor = c.UrbanTolerance switch
		{
			UrbanTolerance.None => 0.3,
			UrbanTolerance.PassThrough => 0.6,
			UrbanTolerance.Allowed => 1.0,
			_ => 1.0
		};

		var useTrails = c.AvoidUnpaved ? 0.0 : 0.5;
		var highwayBase = c.AvoidHighways ? 0.0 : -1.0;
		var tollBase = c.AvoidTolls ? 0.0 : -1.0;

		double Highways(double d) => highwayBase >= 0 ? highwayBase : d;
		double Tolls(double d) => tollBase >= 0 ? tollBase : d;

		return new (string Name, ValhallaMotorcycleOptions Moto)[]
		{
			// Variant 1 — twisty-hills: strong hill preference (0.8, not 1.0).
			// 1.0 caused dead-end and backtrack artifacts. 0.8 still produces
			// hilly/curvy routes while requiring Valhalla to maintain road connectivity.
			("twisty-hills", new ValhallaMotorcycleOptions(
				UseHighways:      Highways(0.05),
				UseTolls:         Tolls(0.3),
				UseTrails:        useTrails,
				UseLivingStreets: livingStreets,
				UseHills:         0.8,
				ServiceFactor:    serviceFactor)),

			// Variant 2 — scenic-valley: moderate hill preference (0.6), allows minor
			// highways. Finds river-valley roads (Meuse, Ourthe, Semois) without
			// insisting on every available gradient.
			("scenic-valley", new ValhallaMotorcycleOptions(
				UseHighways:      Highways(0.2),
				UseTolls:         Tolls(0.5),
				UseTrails:        useTrails,
				UseLivingStreets: livingStreets,
				UseHills:         0.6,
				ServiceFactor:    serviceFactor)),

			// Variant 3 — explorer: balanced hills (0.5), zero tolls, slight unpaved
			// tolerance. Discovers secondary roads and farm tracks the other variants
			// skip without producing hill-chasing artifacts.
			("explorer", new ValhallaMotorcycleOptions(
				UseHighways:      Highways(0.15),
				UseTolls:         Tolls(0.0),
				UseTrails:        Math.Max(useTrails, 0.2),
				UseLivingStreets: livingStreets,
				UseHills:         0.5,
				ServiceFactor:    serviceFactor))
		};
	}

	#endregion

	#region Private — Location building

	private static IReadOnlyList<ValhallaLocation> BuildLocations(RoutePlan plan, double? intermediateOffsetBearing)
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

	/// <summary>
	/// Builds return leg locations: End → perpendicular_midpoint → Start.
	/// The perpendicular offset is 20% of the straight-line distance (reduced from 30%)
	/// to keep the waypoint within the main road network and avoid dead-end terrain.
	/// </summary>
	private static IReadOnlyList<ValhallaLocation> BuildRoundTripReturnLocations(
		RoutePlan plan,
		int perpendicularDirection)
	{
		var end = plan.End!;
		var start = plan.Start;

		var midLat = (end.Latitude + start.Latitude) / 2.0;
		var midLon = (end.Longitude + start.Longitude) / 2.0;
		var midpoint = GeoCoordinate.Create(midLat, midLon).Value;

		var bearingEndToStart = Bearing(end, start);
		var perpendicularBearing = (bearingEndToStart + 90.0 * perpendicularDirection + 360.0) % 360.0;

		var straightLineMeters = HaversineMeters(end, start);
		var offsetMeters = Math.Clamp(
			straightLineMeters * PerpendicularOffsetRatio,
			MinPerpendicularOffsetMeters,
			MaxPerpendicularOffsetMeters);

		var perpendicularPoint = ComputeOffsetCoordinate(midpoint, perpendicularBearing, offsetMeters);

		var list = new List<ValhallaLocation>
		{
			new(end.Latitude,                end.Longitude,                "break"),
			new(perpendicularPoint.Lat,      perpendicularPoint.Lon,       "through"),
			new(start.Latitude,              start.Longitude,              "break")
		};

		// Reverse waypoints (if any) for the return direction
		var waypointList = plan.Waypoints.Reverse().ToList();
		if (waypointList.Count > 0)
		{
			list.RemoveAt(list.Count - 1);
			foreach (var wp in waypointList)
				list.Add(new ValhallaLocation(wp.Location.Latitude, wp.Location.Longitude, "through"));
			list.Add(new ValhallaLocation(start.Latitude, start.Longitude, "break"));
		}

		return list;
	}

	#endregion

	#region Private — Execution

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
			catch (HttpRequestException)
			{
				// Skip — no path for these constraints/waypoints.
			}
		}

		return results;
	}

	#endregion

	#region Private — Exclusion polygon (DifferentRoute loops only)

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

		var ring = new double[][]
		{
			new[] { minLon, minLat },
			new[] { maxLon, minLat },
			new[] { maxLon, maxLat },
			new[] { minLon, maxLat },
			new[] { minLon, minLat }
		};

		return new List<double[][]> { ring };
	}

	#endregion

	#region Private — Geodesy helpers

	private static (double Lat, double Lon) ComputeOffsetCoordinate(
		GeoCoordinate origin, double bearingDegrees, double distanceMeters)
	{
		var lat1 = ToRad(origin.Latitude);
		var lon1 = ToRad(origin.Longitude);
		var bearing = ToRad(bearingDegrees);
		var angDist = distanceMeters / EarthRadiusMeters;

		var lat2 = Math.Asin(
			Math.Sin(lat1) * Math.Cos(angDist) +
			Math.Cos(lat1) * Math.Sin(angDist) * Math.Cos(bearing));

		var lon2 = lon1 + Math.Atan2(
			Math.Sin(bearing) * Math.Sin(angDist) * Math.Cos(lat1),
			Math.Cos(angDist) - Math.Sin(lat1) * Math.Sin(lat2));

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
		var lat1 = ToRad(a.Latitude);
		var lat2 = ToRad(b.Latitude);
		var sinDLat = Math.Sin(dLat / 2);
		var sinDLon = Math.Sin(dLon / 2);
		var h = sinDLat * sinDLat + Math.Cos(lat1) * Math.Cos(lat2) * sinDLon * sinDLon;
		return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));
	}

	private static double ToRad(double d) => d * Math.PI / 180.0;
	private static double ToDeg(double r) => r * 180.0 / Math.PI;

	#endregion
}