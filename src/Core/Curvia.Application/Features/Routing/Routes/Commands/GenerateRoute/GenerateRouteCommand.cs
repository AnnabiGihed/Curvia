using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute.Responses;

namespace Curvia.Application.Features.Routing.Routes.Commands.GenerateRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Command requesting the generation of a fun-optimized motorcycle route.
///              Supports both point-to-point (EndLatitude/EndLongitude set) and loop routes
///              (LoopTargetDistanceMeters set). Exactly one mode must be active.
/// </summary>
public sealed record GenerateRouteCommand(double StartLatitude, double StartLongitude, double? EndLatitude, double? EndLongitude, double? LoopTargetDistanceMeters, IReadOnlyList<WaypointInput>? Waypoints, bool AvoidHighways, bool AvoidTolls, double MaxDetourRatio, double? MaxDistanceMeters, double FunFactor, double CurvesWeight, double ElevationWeight, double SceneryWeight) : ICommand<GenerateRouteResponse>;

public sealed record WaypointInput(double Latitude, double Longitude);