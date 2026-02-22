using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed identifier for the <see cref="MotorcycleMaker"/> aggregate.
///              Wraps a Guid to prevent accidental ID mixing across aggregates.
///              Provides a factory method for generating new identifiers.
/// </summary>
public sealed record MotorcycleMakerId(Guid Value) : StronglyTypedGuidId<MotorcycleMakerId>(Value)
{
	#region Factory
	/// <summary>
	/// Generates a new unique <see cref="MotorcycleMakerId"/>.
	/// </summary>
	public static MotorcycleMakerId New() => new(Guid.NewGuid());
	#endregion
}