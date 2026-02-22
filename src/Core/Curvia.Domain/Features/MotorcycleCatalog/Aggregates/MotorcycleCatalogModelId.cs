using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Named MotorcycleCatalogModelId (not MotorcycleModelId) to avoid collision
/// with the MotorcycleModel value object in Curvia.Domain.Features.Motorcycles.ValueObjects.
/// </summary>
public sealed record MotorcycleCatalogModelId(Guid Value) : StronglyTypedGuidId<MotorcycleCatalogModelId>(Value)
{
	public static MotorcycleCatalogModelId New() => new(Guid.NewGuid());
}