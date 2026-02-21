using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.SavedRoutes.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed GUID identifier for the <see cref="SavedRoute"/> aggregate root.
/// </summary>
public sealed record SavedRouteId(Guid Value) : StronglyTypedGuidId<SavedRouteId>(Value);