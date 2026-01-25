using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Strongly-typed identifier for RoutePlan aggregate.
/// </summary>
public sealed record RoutePlanId : StronglyTypedGuidId<RoutePlanId>
{
	#region Constructor
	public RoutePlanId(Guid value) : base(value) { }
	#endregion

	#region Factory
	public static RoutePlanId New() => new(Guid.NewGuid());
	#endregion
}