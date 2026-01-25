using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Routing.Routes.Aggregate;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Strongly-typed identifier for Route aggregate.
/// </summary>
public sealed record RouteId : StronglyTypedGuidId<RouteId>
{
	#region Constructor
	public RouteId(Guid value) : base(value) { }
	#endregion

	#region Factory
	public static RouteId New() => new(Guid.NewGuid());
	#endregion
}