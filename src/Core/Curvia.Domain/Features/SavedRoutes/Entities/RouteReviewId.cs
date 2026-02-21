using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.SavedRoutes.Entities;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed GUID identifier for the <see cref="RouteReview"/> entity.
/// </summary>
public sealed record RouteReviewId(Guid Value) : StronglyTypedGuidId<RouteReviewId>(Value);