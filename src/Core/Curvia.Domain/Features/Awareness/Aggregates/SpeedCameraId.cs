using Pivot.Framework.Domain.Primitives;

namespace Curvia.Domain.Features.Awareness.Aggregates;

public sealed record SpeedCameraId(Guid Value) : StronglyTypedGuidId<SpeedCameraId>(Value)
{
	#region Factory
	/// <summary>
	/// Generates a new unique <see cref="SpeedCameraId"/>.
	/// </summary>
	public static SpeedCameraId New() => new(Guid.NewGuid());
	#endregion
}