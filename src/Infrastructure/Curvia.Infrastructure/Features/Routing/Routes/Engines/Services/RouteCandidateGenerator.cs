using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Generates Valhalla route candidates for a given <see cref="RoutePlan"/>.
///              Produces 3 named variants per call (balanced, scenic, explorer) that differ
///              in highway/toll/surface preferences so the scoring service can pick the best one.
///              For loop plans: adds a computed intermediate waypoint at half-radius distance
///              to force Valhalla to leave the start area before returning.
///              For DifferentRoute loops: <see cref="GenerateReturnAsync"/> repeats the process
///              with an exclude_polygons entry built from the outbound route's bounding box.
/// </summary>
internal sealed class RouteCandidateGenerator : IRouteCandidateGenerator
{
	#region Fields
	private readonly IValhallaRoutingClient _client;
	/// <summary>
	/// Earth's mean radius in meters — used for bearing/offset calculations.
	/// </summary>
	private const double EarthRadiusMeters = 6_371_000.0;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises the generator with the Valhalla HTTP client.
	/// </summary>
	/// <param name="client">Valhalla routing client.</param>
	public RouteCandidateGenerator(IValhallaRoutingClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}
	#endregion

	#region IRouteCandidateGenerator — Outbound
	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateAsync(RoutePlan plan, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);

		var variants = BuildVariants(plan);
		var locations = BuildLocations(plan, intermediateOffsetBearing: null);

		return await ExecuteVariantsAsync(locations, variants, excludePolygons: null, cancellationToken);
	}
	#endregion

	#region Private — Execution
	/// <summary>
	/// Sends one Valhalla /route request per variant and collects the results.
	/// Failed variants are logged and skipped rather than throwing, so partial results are returned.
	/// </summary>
	private async Task<IReadOnlyList<RouteCandidate>> ExecuteVariantsAsync(IReadOnlyList<ValhallaLocation> locations, (string Name, ValhallaMotorcycleOptions Moto)[] variants, IReadOnlyList<ExcludePolygon>? excludePolygons, CancellationToken cancellationToken)
	{
		var results = new List<RouteCandidate>(variants.Length);

		foreach (var (name, moto) in variants)
		{
			try
			{
				var req = new ValhallaRouteRequest(Locations: locations, Costing: "motorcycle", CostingOptions: new ValhallaCostingOptions(moto), DirectionsOptions: new ValhallaDirectionsOptions(), ExcludePolygons: excludePolygons);

				var raw = await _client.RouteAsync(req, cancellationToken);
				results.Add(new RouteCandidate(raw, name));
			}
			catch (HttpRequestException)
			{
				// Skip this variant — Valhalla may have no path for these constraints.
				// The scoring service will naturally prefer the variants that succeeded.
			}
		}

		return results;
	}
	#endregion

	#region Private — Haversine offset
	private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
	private static double ToDeg(double radians) => radians * 180.0 / Math.PI;

	/// <summary>
	/// Computes a coordinate offset from a start point at a given bearing and distance.
	/// Uses the spherical Earth model (Haversine).
	/// </summary>
	/// <param name="origin">Starting coordinate.</param>
	/// <param name="bearingDegrees">Compass bearing in degrees (0 = North, 90 = East).</param>
	/// <param name="distanceMeters">Distance to travel from origin in meters.</param>
	/// <returns>The destination coordinate as a (lat, lon) tuple.</returns>
	private static (double Lat, double Lon) ComputeOffsetCoordinate(GeoCoordinate origin, double bearingDegrees, double distanceMeters)
	{
		var lat1 = ToRad(origin.Latitude);
		var lon1 = ToRad(origin.Longitude);
		var bearing = ToRad(bearingDegrees);
		var angDist = distanceMeters / EarthRadiusMeters;

		var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(angDist) + Math.Cos(lat1) * Math.Sin(angDist) * Math.Cos(bearing));

		var lon2 = lon1 + Math.Atan2(Math.Sin(bearing) * Math.Sin(angDist) * Math.Cos(lat1), Math.Cos(angDist) - Math.Sin(lat1) * Math.Sin(lat2));

		return (ToDeg(lat2), ToDeg(lon2));
	}
	#endregion

	#region Private — Location building
	/// <summary>
	/// Builds the ordered list of Valhalla locations for the request.
	/// For point-to-point: start → (waypoints) → end.
	/// For loop: start → intermediate offset point → start (forces the route to leave the area).
	/// </summary>
	/// <param name="plan">Routing plan.</param>
	/// <param name="intermediateOffsetBearing">
	/// For return loop only: bearing (degrees) at which to place the intermediate waypoint,
	/// overriding the default East (90°) bearing used for the outbound loop.
	/// Null = use default East bearing for outbound loops.
	/// </param>
	private static IReadOnlyList<ValhallaLocation> BuildLocations(RoutePlan plan, double? intermediateOffsetBearing)
	{
		var list = new List<ValhallaLocation>();

		// Origin — always a hard stop
		list.Add(new ValhallaLocation(plan.Start.Latitude, plan.Start.Longitude, "break"));

		if (plan.LoopSpec is not null)
		{
			// For loops: add an intermediate "through" point to force Valhalla
			// to venture away from start before looping back.
			// The intermediate point is placed at ~loop_radius distance from start.
			var bearing = intermediateOffsetBearing ?? 90.0; // Default: East
			var offset = ComputeOffsetCoordinate(plan.Start, bearingDegrees: bearing, distanceMeters: plan.LoopSpec.TargetDistance.Meters / (2 * Math.PI));

			list.Add(new ValhallaLocation(offset.Lat, offset.Lon, "through"));

			// Close the loop: end = start
			list.Add(new ValhallaLocation(plan.Start.Latitude, plan.Start.Longitude, "break"));
		}
		else
		{
			// Point-to-point: optional waypoints then destination
			foreach (var wp in plan.Waypoints)
				list.Add(new ValhallaLocation(wp.Location.Latitude, wp.Location.Longitude, "through"));

			if (plan.End is not null)
				list.Add(new ValhallaLocation(plan.End.Latitude, plan.End.Longitude, "break"));
		}

		return list;
	}
	#endregion

	#region Private — Variant definitions
	/// <summary>
	/// Builds the 3 costing variants adjusted for the plan's hard constraints.
	/// </summary>
	/// <param name="plan">The routing plan (constraints drive hard overrides).</param>
	/// <returns>Array of named variant tuples.</returns>
	private static (string Name, ValhallaMotorcycleOptions Moto)[] BuildVariants(RoutePlan plan)
	{
		var c = plan.Constraints;

		// Map UrbanTolerance to Valhalla's use_living_streets float
		var livingStreets = c.UrbanTolerance switch
		{
			UrbanTolerance.None => 0.0,
			UrbanTolerance.PassThrough => 0.2,
			UrbanTolerance.Allowed => 0.6,
			_ => 0.6
		};

		// Map AvoidUnpaved to Valhalla's use_trails float
		// 0.0 = avoid trails/unpaved, 0.5 = neutral, 1.0 = prefer
		var useTrails = c.AvoidUnpaved ? 0.0 : 0.5;

		// Hard overrides when the rider has explicit avoid flags
		var highwayBase = c.AvoidHighways ? 0.0 : -1.0; // -1 means "use variant default"
		var tollBase = c.AvoidTolls ? 0.0 : -1.0;

		double Highways(double defaultVal) => highwayBase >= 0 ? highwayBase : defaultVal;
		double Tolls(double defaultVal) => tollBase >= 0 ? tollBase : defaultVal;

		return new (string Name, ValhallaMotorcycleOptions Moto)[]
		{
			// Variant 1 — balanced: moderate highway use, full toll acceptance
			("balanced", new ValhallaMotorcycleOptions(UseHighways: Highways(0.7), UseTolls: Tolls(0.9), UseTrails: useTrails, UseLivingStreets: livingStreets)),

			// Variant 2 — scenic: strongly prefers minor/secondary roads, some tolls OK
			("scenic", new ValhallaMotorcycleOptions(UseHighways: Highways(0.1), UseTolls: Tolls(0.5), UseTrails: useTrails, UseLivingStreets: livingStreets)),

			// Variant 3 — explorer: avoid highways AND tolls, widest detour accepted
			("explorer", new ValhallaMotorcycleOptions(UseHighways: Highways(0.2), UseTolls: Tolls(0.0), UseTrails: useTrails, UseLivingStreets: livingStreets))
		};
	}
	#endregion

	#region IRouteCandidateGenerator — Return Leg
	/// <inheritdoc/>
	public async Task<IReadOnlyList<RouteCandidate>> GenerateReturnAsync(RoutePlan plan, ValhallaRouteResponse outboundResponse, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);
		ArgumentNullException.ThrowIfNull(outboundResponse);

		var variants = BuildVariants(plan);

		// Use a different intermediate offset for the return (opposite side of the start point)
		// to encourage a genuinely different circular path.
		var locations = BuildLocations(plan, intermediateOffsetBearing: 180.0);

		// Build an exclude polygon from the outbound route's bounding box
		// (slightly enlarged by 10 % to avoid edge-case overlaps).
		var excludePolygons = BuildExcludePolygons(outboundResponse);

		return await ExecuteVariantsAsync(locations, variants, excludePolygons, cancellationToken);
	}
	#endregion

	#region Private — Exclusion polygon (DifferentRoute)
	/// <summary>
	/// Builds an <c>exclude_polygons</c> list from the outbound route's bounding box.
	/// The polygon is the bounding box enlarged by 10 % on each side,
	/// encoded as a GeoJSON-style list of [lon, lat] ring coordinates.
	/// </summary>
	private static IReadOnlyList<ExcludePolygon> BuildExcludePolygons(ValhallaRouteResponse outbound)
	{
		if (outbound.Trip?.Summary is null)
			return Array.Empty<ExcludePolygon>();

		// Decode all shape points across all legs to get a tight bounding box
		var allPoints = outbound.Trip.Legs
			.SelectMany(leg => Polyline6Decoder.Decode(leg.Shape))
			.ToList();

		if (allPoints.Count == 0)
			return Array.Empty<ExcludePolygon>();

		var minLat = allPoints.Min(p => p.Latitude);
		var maxLat = allPoints.Max(p => p.Latitude);
		var minLon = allPoints.Min(p => p.Longitude);
		var maxLon = allPoints.Max(p => p.Longitude);

		// Expand bounding box by 10 % on each axis
		var latPad = (maxLat - minLat) * 0.10;
		var lonPad = (maxLon - minLon) * 0.10;

		minLat -= latPad;
		maxLat += latPad;
		minLon -= lonPad;
		maxLon += lonPad;

		// Valhalla expects exclude_polygons as list of rings: each ring = list of [lon, lat]
		var ring = new[]
		{
			new[] { minLon, minLat },
			new[] { maxLon, minLat },
			new[] { maxLon, maxLat },
			new[] { minLon, maxLat },
			new[] { minLon, minLat }  // close the ring
		};

		return new List<ExcludePolygon> { new ExcludePolygon(ring) };
	}
	#endregion
}