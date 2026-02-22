using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

public sealed record MotorcycleMakerId(Guid Value) : StronglyTypedGuidId<MotorcycleMakerId>(Value)
{
	public static MotorcycleMakerId New() => new(Guid.NewGuid());
}