namespace Curvia.Application.Features.Routes.Commands.Generate.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Bounding box DTO embedded in <see cref="GenerateRouteResponse"/> and
///              <see cref="ReturnLegDto"/>. Property names follow the geographic standard
///              (South/West/North/East) consistent with <see cref="Curvia.Domain.Shared.ValueObjects.BoundingBox"/>.
/// </summary>
/// <param name="South">Southern latitude boundary (min latitude).</param>
/// <param name="West">Western longitude boundary (min longitude).</param>
/// <param name="North">Northern latitude boundary (max latitude).</param>
/// <param name="East">Eastern longitude boundary (max longitude).</param>
public sealed record BoundingBoxDto(double South, double West, double North, double East);