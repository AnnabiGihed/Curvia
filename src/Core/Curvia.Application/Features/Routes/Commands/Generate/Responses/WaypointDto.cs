using Curvia.Application.Features.Routes.Commands.Generate;

namespace Curvia.Application.Features.Routes.Commands.Generate.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Intermediate waypoint DTO used in <see cref="GenerateRouteCommand"/>.
/// </summary>
/// <param name="Latitude">Waypoint latitude in decimal degrees (WGS84).</param>
/// <param name="Longitude">Waypoint longitude in decimal degrees (WGS84).</param>
public sealed record WaypointDto(double Latitude, double Longitude);