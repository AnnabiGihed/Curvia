using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Routing.RoutePlans.Projections;

namespace Curvia.Domain.Features.Routing.Routes.Projections;

public sealed record RouteProjectionId : StronglyTypedGuidId<RouteProjectionId>
{
	#region Constructor
	public RouteProjectionId(Guid value) : base(value) { }
	#endregion
}
