using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Application.Features.Awareness.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.Awareness.Queries.GetAwarenessForArea;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Returns awareness items within a WGS84 bounding box.
///              Unauthenticated endpoint — data is public.
///
///              MaxAreaDegrees cap prevents unbounded queries from the public API.
///              Types filter allows mobile clients to request only the categories
///              they need (e.g. cameras only = one repository call).
/// </summary>
public sealed record GetAwarenessForAreaQuery(double MinLat, double MinLon, double MaxLat, double MaxLon, AwarenessItemType[] Types) : IQuery<AwarenessResultDto>;