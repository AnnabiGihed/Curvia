namespace Curvia.Application.Features.Routes.Commands.Generate.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Bounding box DTO embedded in <see cref="GenerateRouteResponse"/>.
/// </summary>
/// <param name="MinLatitude">South boundary.</param>
/// <param name="MinLongitude">West boundary.</param>
/// <param name="MaxLatitude">North boundary.</param>
/// <param name="MaxLongitude">East boundary.</param>
public sealed record BoundingBoxDto(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude);