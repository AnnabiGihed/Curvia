using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Routes.Entities;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

internal sealed class RouteBuilder : IRouteBuilder
{
	/// <summary>
	/// Author      : Gihed Annabi
	/// Date        : 01-2026
	/// Purpose     : Builds the Curvia domain Route aggregate from a Valhalla response.
	///              - Decodes Valhalla polyline6 shapes
	///              - Creates Polyline, BoundingBox, RouteStats and at least one RouteSegment
	///              - Produces a fully valid Route aggregate ready for persistence
	/// </summary>
	public Result<Route> Build(RoutePlan plan, ValhallaRouteResponse response, string graphVersionId)
	{
		#region Guards

		if (plan is null) return Result.Failure<Route>(Error.NullValue);
		if (response?.Trip is null) return Result.Failure<Route>(Error.NullValue);
		if (string.IsNullOrWhiteSpace(graphVersionId)) return Result.Failure<Route>(Error.NullValue);

		#endregion

		#region Extract Valhalla core data

		var trip = response.Trip;

		if (trip.Legs is null || trip.Legs.Count == 0)
			return Result.Failure<Route>(new Error("Routing.Valhalla.NoLegs", "Valhalla response contains no legs."));

		var shape = trip.Legs[0].Shape;
		if (string.IsNullOrWhiteSpace(shape))
			return Result.Failure<Route>(new Error("Routing.Valhalla.NoShape", "Valhalla response contains no route shape."));

		#endregion

		#region Decode polyline6 -> GeoCoordinate list

		var points = Polyline6Decoder.Decode(shape);

		if (points.Count < 2)
			return Result.Failure<Route>(new Error("Routing.Valhalla.ShapeInvalid", "Decoded polyline has fewer than 2 points."));

		#endregion

		#region Domain ValueObjects

		var polylineResult = Polyline.Create(points);
		if (polylineResult.IsFailure) return Result.Failure<Route>(polylineResult.Error, polylineResult.ResultExceptionType);

		var bboxResult = BoundingBox.FromPoints(points);
		if (bboxResult.IsFailure) return Result.Failure<Route>(bboxResult.Error, bboxResult.ResultExceptionType);

		// Valhalla length is in kilometers when units=kilometers; convert to meters.
		var distanceResult = Distance.Create(trip.Summary.Length * 1000.0);
		if (distanceResult.IsFailure) return Result.Failure<Route>(distanceResult.Error, distanceResult.ResultExceptionType);

		var durationResult = Duration.Create((long)Math.Round(trip.Summary.Time));
		if (durationResult.IsFailure) return Result.Failure<Route>(durationResult.Error, durationResult.ResultExceptionType);

		var statsResult = RouteStats.Create(distanceResult.Value, durationResult.Value);
		if (statsResult.IsFailure) return Result.Failure<Route>(statsResult.Error, statsResult.ResultExceptionType);

		#endregion

		#region Segments (V1: single segment = whole route)

		var segStatsResult = RouteStats.Create(distanceResult.Value, durationResult.Value);
		if (segStatsResult.IsFailure) return Result.Failure<Route>(segStatsResult.Error, segStatsResult.ResultExceptionType);

		var segResult = RouteSegment.Create(polylineResult.Value, segStatsResult.Value);
		if (segResult.IsFailure) return Result.Failure<Route>(segResult.Error, segResult.ResultExceptionType);

		var segments = new List<RouteSegment> { segResult.Value };

		#endregion

		#region Create aggregate

		var routeResult = Route.Create(
			routePlanId: plan.Id,
			graphVersionId: graphVersionId,
			geometry: polylineResult.Value,
			boundingBox: bboxResult.Value,
			stats: statsResult.Value,
			segments: segments);

		if (routeResult.IsFailure)
			return Result.Failure<Route>(routeResult.Error, routeResult.ResultExceptionType);

		return Result.Success(routeResult.Value);

		#endregion
	}
}