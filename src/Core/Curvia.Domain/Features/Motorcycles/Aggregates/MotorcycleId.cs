using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Motorcycles.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed GUID identifier for the <see cref="Motorcycle"/> aggregate root.
/// </summary>
public sealed record MotorcycleId(Guid Value) : StronglyTypedGuidId<MotorcycleId>(Value)
{
	public static MotorcycleId New() => new(Guid.NewGuid());
}