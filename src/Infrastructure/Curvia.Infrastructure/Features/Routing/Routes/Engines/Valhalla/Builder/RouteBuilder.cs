using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Shared.ValueObjects;
using Curvia.Domain.Features.Routing.Routes.Entities;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Infrastructure.Features.Routing.Routes.Helpers;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.Builder;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.Builder;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Builds the Curvia domain Route aggregate from a Valhalla response.
///
///              Multi-leg handling:
///              Valhalla returns one leg per waypoint interval (e.g. a route with 2 intermediate
///              waypoints produces 3 legs). Each leg has its own shape (polyline6), summary
///              (distance + time), and maneuvers. This builder:
///
///                1. Decodes every leg's shape into GeoCoordinate lists.
///                2. Deduplicates junction points — the last point of leg N is identical to the
///                   first point of leg N+1 in Valhalla's encoding. Keeping both would introduce
///                   zero-length segments and corrupt curvature analysis on the merged geometry.
///                3. Creates one RouteSegment per leg, carrying the leg's own geometry and
///                   per-leg distance/duration from the leg summary.
///                4. Merges all leg points into a single master Polyline for the Route aggregate.
///                5. Derives the Route-level BoundingBox and RouteStats from the authoritative
///                   trip.Summary values (not from summing legs, which can have floating-point drift).
/// </summary>
internal sealed class RouteBuilder : IRouteBuilder
{
	/// <inheritdoc/>
	public Result<Route> Build(RoutePlan plan, ValhallaRouteResponse response, string graphVersionId)
	{
		#region Guards
		if (plan is null) return Result.Failure<Route>(Error.NullValue);
		if (response?.Trip is null) return Result.Failure<Route>(Error.NullValue);
		if (string.IsNullOrWhiteSpace(graphVersionId)) return Result.Failure<Route>(Error.NullValue);
		#endregion

		var trip = response.Trip;

		if (trip.Legs is null || trip.Legs.Count == 0)
			return Result.Failure<Route>(new Error("Routing.Valhalla.NoLegs", "Valhalla response contains no legs."));

		#region Decode all legs
		// Each leg decodes to its own list of GeoCoordinates.
		// We validate every leg individually so partial failures are surfaced clearly.
		var legPoints = new List<List<GeoCoordinate>>(trip.Legs.Count);

		for (var i = 0; i < trip.Legs.Count; i++)
		{
			var leg = trip.Legs[i];

			if (string.IsNullOrWhiteSpace(leg.Shape))
				return Result.Failure<Route>(new Error("Routing.Valhalla.NoShape", $"Valhalla leg {i} contains no route shape."));

			var points = Polyline6Decoder.Decode(leg.Shape);

			if (points.Count < 2)
				return Result.Failure<Route>(new Error("Routing.Valhalla.ShapeInvalid", $"Decoded polyline for leg {i} has fewer than 2 points."));

			legPoints.Add(points);
		}
		#endregion

		#region Build per-leg RouteSegments
		// One RouteSegment per Valhalla leg.
		// Each segment carries the leg's own geometry and its per-leg distance/duration.
		var segments = new List<RouteSegment>(trip.Legs.Count);

		for (var i = 0; i < trip.Legs.Count; i++)
		{
			var leg = trip.Legs[i];
			var points = legPoints[i];

			var legPolylineResult = Polyline.Create(points);
			if (legPolylineResult.IsFailure)
				return Result.Failure<Route>(legPolylineResult.Error, legPolylineResult.ResultExceptionType);

			// Per-leg stats from the leg's own summary (authoritative for this interval)
			var legDistResult = Distance.Create(leg.Summary.Length * 1000.0);
			if (legDistResult.IsFailure)
				return Result.Failure<Route>(legDistResult.Error, legDistResult.ResultExceptionType);

			var legDurResult = Duration.Create((long)Math.Round(leg.Summary.Time));
			if (legDurResult.IsFailure)
				return Result.Failure<Route>(legDurResult.Error, legDurResult.ResultExceptionType);

			var legStatsResult = RouteStats.Create(legDistResult.Value, legDurResult.Value);
			if (legStatsResult.IsFailure)
				return Result.Failure<Route>(legStatsResult.Error, legStatsResult.ResultExceptionType);

			var segResult = RouteSegment.Create(legPolylineResult.Value, legStatsResult.Value);
			if (segResult.IsFailure)
				return Result.Failure<Route>(segResult.Error, segResult.ResultExceptionType);

			segments.Add(segResult.Value);
		}
		#endregion

		#region Merge all leg points into a single master polyline
		// Valhalla encodes the junction point (end of leg N = start of leg N+1) in both legs.
		// We must skip the first point of every leg after the first to avoid duplicates.
		// These duplicates would produce zero-length segments in curvature analysis and
		// corrupt the bounding box calculation with redundant identical points.
		var allPoints = new List<GeoCoordinate>(
			legPoints.Sum(lp => lp.Count));

		for (var i = 0; i < legPoints.Count; i++)
		{
			var lp = legPoints[i];

			// First leg: take all points.
			// Subsequent legs: skip index 0 (it is the same coordinate as the last point of the previous leg).
			var startIndex = i == 0 ? 0 : 1;

			for (var j = startIndex; j < lp.Count; j++)
				allPoints.Add(lp[j]);
		}

		if (allPoints.Count < 2)
			return Result.Failure<Route>(new Error("Routing.Valhalla.MergedPolylineInvalid", "Merged route polyline has fewer than 2 points after leg deduplication."));
		#endregion

		#region Route-level ValueObjects
		var polylineResult = Polyline.Create(allPoints);
		if (polylineResult.IsFailure)
			return Result.Failure<Route>(polylineResult.Error, polylineResult.ResultExceptionType);

		var bboxResult = BoundingBox.FromPoints(allPoints);
		if (bboxResult.IsFailure)
			return Result.Failure<Route>(bboxResult.Error, bboxResult.ResultExceptionType);

		// Route-level distance and duration come from trip.Summary — this is the authoritative
		// source and avoids floating-point drift from summing individual leg values.
		var distResult = Distance.Create(trip.Summary.Length * 1000.0);
		if (distResult.IsFailure)
			return Result.Failure<Route>(distResult.Error, distResult.ResultExceptionType);

		var durResult = Duration.Create((long)Math.Round(trip.Summary.Time));
		if (durResult.IsFailure)
			return Result.Failure<Route>(durResult.Error, durResult.ResultExceptionType);

		var statsResult = RouteStats.Create(distResult.Value, durResult.Value);
		if (statsResult.IsFailure)
			return Result.Failure<Route>(statsResult.Error, statsResult.ResultExceptionType);
		#endregion

		#region Create Route aggregate
		var routeResult = Route.Create(routePlanId: plan.Id, graphVersionId: graphVersionId, geometry: polylineResult.Value, boundingBox: bboxResult.Value, stats: statsResult.Value, segments: segments);

		if (routeResult.IsFailure)
			return Result.Failure<Route>(routeResult.Error, routeResult.ResultExceptionType);

		return Result.Success(routeResult.Value);
		#endregion
	}
}