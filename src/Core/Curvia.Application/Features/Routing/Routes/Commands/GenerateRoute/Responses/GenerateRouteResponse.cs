namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Response DTO returned by GenerateRouteCommand.
///              Carries the winning route candidate back to the API layer.
/// </summary>
public sealed record GenerateRouteResponse(Guid RouteId, string VariantName, double DistanceMeters, long DurationSeconds, double FunScore, IReadOnlyList<CoordinateResponse> Polyline, BoundingBoxResponse BoundingBox);