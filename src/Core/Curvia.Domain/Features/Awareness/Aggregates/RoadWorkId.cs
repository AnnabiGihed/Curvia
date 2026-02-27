using Templates.Core.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;

public sealed record RoadWorkId(Guid Value) : StronglyTypedGuidId<RoadWorkId>(Value)
{
	#region Factory
	/// <summary>
	/// Generates a new unique <see cref="RoadWorkId"/>.
	/// </summary>
	public static RoadWorkId New() => new(Guid.NewGuid());
	#endregion
}