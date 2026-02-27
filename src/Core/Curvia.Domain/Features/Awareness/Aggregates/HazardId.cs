using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;


public sealed record HazardId(Guid Value) : StronglyTypedGuidId<HazardId>(Value)
{
	#region Factory
	/// <summary>
	/// Generates a new unique <see cref="HazardId"/>.
	/// </summary>
	public static HazardId New() => new(Guid.NewGuid());
	#endregion
}