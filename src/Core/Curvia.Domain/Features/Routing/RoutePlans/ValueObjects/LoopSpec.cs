using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a loop (roundtrip) route request originating and ending at the same point.
///              Contains the target loop distance and the strategy used to generate the return leg.
///              Mutually exclusive with a point-to-point End coordinate on <see cref="RoutePlan"/>.
/// </summary>
public sealed class LoopSpec : CSharpFunctionalExtensions.ValueObject<LoopSpec>
{
	#region Properties

	/// <summary>Sentinel flag persisted to EF. Always true when this object exists.</summary>
	public bool IsLoop { get; private set; } = true;

	/// <summary>Target total distance for the loop in meters.</summary>
	public Distance TargetDistance { get; private set; } = default!;

	/// <summary>
	/// Controls whether the return leg reuses the same roads (<see cref="ReturnStrategy.SameRoute"/>)
	/// or generates a completely different set of roads (<see cref="ReturnStrategy.DifferentRoute"/>).
	/// </summary>
	public ReturnStrategy ReturnStrategy { get; private set; }

	#endregion

	#region Constructors

	/// <summary>For EF Core.</summary>
	private LoopSpec() { }

	private LoopSpec(Distance targetDistance, ReturnStrategy returnStrategy)
	{
		IsLoop = true;
		TargetDistance = targetDistance;
		ReturnStrategy = returnStrategy;
	}

	#endregion

	#region Factory

	/// <summary>
	/// Creates a validated <see cref="LoopSpec"/>.
	/// </summary>
	/// <param name="targetDistance">Total loop distance. Minimum 1,000 m. Maximum 500,000 m.</param>
	/// <param name="returnStrategy">
	/// How the return leg is generated.
	/// Default: <see cref="ReturnStrategy.SameRoute"/> (circular loop).
	/// </param>
	public static Result<LoopSpec> Create(Distance targetDistance, ReturnStrategy returnStrategy = ReturnStrategy.SameRoute)
	{
		if (targetDistance is null)
			return Result.Failure<LoopSpec>(RoutingErrors.NullValue(nameof(targetDistance)));

		if (targetDistance.Meters < 1_000)
			return Result.Failure<LoopSpec>(new Error("Routing.LoopSpec.TooShort", "Loop target distance must be at least 1,000 meters."));

		if (targetDistance.Meters > 500_000)
			return Result.Failure<LoopSpec>(new Error("Routing.LoopSpec.TooLong", "Loop target distance cannot exceed 500,000 meters (500 km)."));

		if (!Enum.IsDefined(returnStrategy))
			return Result.Failure<LoopSpec>(new Error("Routing.LoopSpec.InvalidReturnStrategy", $"Unknown ReturnStrategy value '{(int)returnStrategy}'."));

		return Result.Success(new LoopSpec(targetDistance, returnStrategy));
	}

	#endregion

	#region Equality

	/// <inheritdoc/>
	protected override bool EqualsCore(LoopSpec other)
		=> TargetDistance.Equals(other.TargetDistance)
		   && ReturnStrategy == other.ReturnStrategy;

	/// <inheritdoc/>
	protected override int GetHashCodeCore()
		=> HashCode.Combine(TargetDistance, ReturnStrategy);

	#endregion
}