namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

public sealed record BoundingBoxResponse(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude);