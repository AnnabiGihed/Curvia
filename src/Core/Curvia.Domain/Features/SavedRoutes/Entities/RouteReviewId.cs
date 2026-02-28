using Pivot.Framework.Domain.Primitives;

namespace Curvia.Domain.Features.SavedRoutes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed GUID identifier for the <see cref="RouteReview"/> entity.
/// </summary>
public sealed record RouteReviewId : StronglyTypedGuidId<RouteReviewId>
{
	#region Constructor
	public RouteReviewId(Guid value) : base(value) { }
	#endregion

	#region Factory
	public static RouteReviewId New() => new(Guid.NewGuid());
	#endregion
}