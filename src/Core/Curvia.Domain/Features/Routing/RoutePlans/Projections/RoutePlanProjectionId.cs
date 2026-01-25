using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Routing.RoutePlans.Projections;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Strongly-typed identifier for RoutePlan response.
/// </summary>
public sealed record RoutePlanProjectionId : StronglyTypedGuidId<RoutePlanProjectionId>
{
	#region Constructor
	public RoutePlanProjectionId(Guid value) : base(value) { }
	#endregion
}
