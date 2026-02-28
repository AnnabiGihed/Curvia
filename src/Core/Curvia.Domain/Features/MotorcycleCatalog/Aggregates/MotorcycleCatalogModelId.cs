using Pivot.Framework.Domain.Primitives;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed identifier for the <see cref="MotorcycleCatalogModel"/> aggregate.
///              Named MotorcycleCatalogModelId (not MotorcycleModelId) to avoid collision
///              with the MotorcycleModel value object in Curvia.Domain.Features.Motorcycles.ValueObjects.
///              Wraps a Guid to enforce type-safety across aggregate boundaries.
/// </summary>
public sealed record MotorcycleCatalogModelId(Guid Value) : StronglyTypedGuidId<MotorcycleCatalogModelId>(Value)
{
	#region Factory
	/// <summary>
	/// Generates a new unique <see cref="MotorcycleCatalogModelId"/>.
	/// </summary>
	public static MotorcycleCatalogModelId New() => new(Guid.NewGuid());
	#endregion
}