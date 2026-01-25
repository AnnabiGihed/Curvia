using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Captures routing constraints applied to route generation.
/// </summary>
public sealed class RoutingConstraints : CSharpFunctionalExtensions.ValueObject<RoutingConstraints>
{
	#region Properties

	public double MaxDetourRatio { get; }
	public bool AvoidHighways { get; }
	public bool AvoidTolls { get; }

	/// <summary>
	/// Optional absolute cap for distance (domain guardrail).
	/// </summary>
	public Distance? MaxDistance { get; }

	#endregion

	#region Constructor

	private RoutingConstraints(double maxDetourRatio, bool avoidHighways, bool avoidTolls, Distance? maxDistance)
	{
		MaxDetourRatio = maxDetourRatio;
		AvoidHighways = avoidHighways;
		AvoidTolls = avoidTolls;
		MaxDistance = maxDistance;
	}

	#endregion

	#region Factory

	public static Result<RoutingConstraints> Create(
		double maxDetourRatio,
		bool avoidHighways,
		bool avoidTolls,
		Distance? maxDistance = null)
	{
		if (double.IsNaN(maxDetourRatio) || double.IsInfinity(maxDetourRatio))
			return Result.Failure<RoutingConstraints>(new Error("Routing.Constraints.NonFinite", "MaxDetourRatio must be a finite number."));

		if (maxDetourRatio < 1.0)
			return Result.Failure<RoutingConstraints>(RoutingErrors.InvalidDetourRatio(maxDetourRatio));

		return Result.Success(new RoutingConstraints(maxDetourRatio, avoidHighways, avoidTolls, maxDistance));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(RoutingConstraints other)
	{
		return MaxDetourRatio.Equals(other.MaxDetourRatio)
			   && AvoidHighways == other.AvoidHighways
			   && AvoidTolls == other.AvoidTolls
			   && Equals(MaxDistance, other.MaxDistance);
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(
			MaxDetourRatio,
			AvoidHighways,
			AvoidTolls,
			MaxDistance);
	}

	#endregion
}