using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a loop route request (start point + target loop distance).
///              You can evolve this later with heading preferences, radius constraints, etc.
/// </summary>
public sealed class LoopSpec : CSharpFunctionalExtensions.ValueObject<LoopSpec>
{
	#region Properties
	public bool IsLoop { get; private set; } = true;
	public Distance TargetDistance { get; private set; }

	#endregion

	#region Constructor
	private LoopSpec() { }
	private LoopSpec(Distance targetDistance)
	{
		IsLoop = true;
		TargetDistance = targetDistance;
	}

	#endregion

	#region Factory

	public static Result<LoopSpec> Create(Distance targetDistance)
	{
		if (targetDistance is null)
			return Result.Failure<LoopSpec>(RoutingErrors.NullValue(nameof(targetDistance)));

		// Domain guardrail: require at least 1km loops
		if (targetDistance.Meters < 1_000)
			return Result.Failure<LoopSpec>(new Error("Routing.LoopSpec.TooShort", "Loop target distance must be at least 1,000 meters."));

		return Result.Success(new LoopSpec(targetDistance));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(LoopSpec other)
		=> TargetDistance.Equals(other.TargetDistance);

	protected override int GetHashCodeCore()
		=> TargetDistance.GetHashCode();

	#endregion
}