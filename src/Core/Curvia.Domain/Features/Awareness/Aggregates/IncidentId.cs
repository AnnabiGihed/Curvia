using Pivot.Framework.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;

public sealed record IncidentId(Guid Value) : StronglyTypedGuidId<IncidentId>(Value)
{
	#region Factory
	/// <summary>
	/// Generates a new unique <see cref="IncidentId"/>.
	/// </summary>
	public static IncidentId New() => new(Guid.NewGuid());
	#endregion
}