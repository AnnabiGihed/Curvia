using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Routing.Routes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Strongly-typed identifier for RouteSegment entity.
/// </summary>
public sealed record RouteSegmentId : StronglyTypedGuidId<RouteSegmentId>
{
	#region Constructor
	public RouteSegmentId(Guid value) : base(value) { }
	#endregion

	#region Factory
	public static RouteSegmentId New() => new(Guid.NewGuid());
	#endregion
}