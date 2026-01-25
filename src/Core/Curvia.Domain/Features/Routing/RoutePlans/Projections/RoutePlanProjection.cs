using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Domain.Features.Routing.RoutePlans.Projections;


/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Read model (projection) for RoutePlan used in query side.
///              Keep it denormalized and UI/API friendly.
/// </summary>
public sealed class RoutePlanProjection : ProjectionRoot<RoutePlanProjectionId>
{
	#region Properties
	public GeoCoordinate? End { get; set; }
	public LoopSpec? LoopSpec { get; set; }
	public GeoCoordinate Start { get; set; } = default!;
	public ScoringProfile ScoringProfile { get; set; } = default!;
	public RoutingConstraints Constraints { get; set; } = default!;
	public List<Waypoint> WayPoints { get; set; } = new();
	#endregion
}
