using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Application.Features.Awareness.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.Awareness.Queries.GetAwarenessForRoute;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Returns awareness items within a corridor around a saved route.
///              Requires authentication — only the route owner (or any authenticated
///              user for public routes) may query the corridor.
///
///              BufferMeters controls the width of the awareness corridor on each side
///              of the route geometry. Default is 500 m; maximum is 2 000 m.
/// </summary>
public sealed record GetAwarenessForRouteQuery(Guid RouteId, double BufferMeters, AwarenessItemType[] Types) : IQuery<AwarenessResultDto>;