using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines scoring configuration used by the routing engine.
/// </summary>
public sealed class ScoringProfile : CSharpFunctionalExtensions.ValueObject<ScoringProfile>
{
	#region Properties

	/// <summary>
	/// Global multiplier applied to the "fun" component in the cost function.
	/// Domain range kept bounded to prevent extreme outputs.
	/// </summary>
	public double FunFactor { get; }

	public ScoringWeights Weights { get; }

	#endregion

	#region Constructor
	private ScoringProfile()
	{
	}
	private ScoringProfile(double funFactor, ScoringWeights weights)
	{
		FunFactor = funFactor;
		Weights = weights;
	}

	#endregion

	#region Factory

	public static Result<ScoringProfile> Create(double funFactor, ScoringWeights weights)
	{
		if (weights is null)
			return Result.Failure<ScoringProfile>(RoutingErrors.NullValue(nameof(weights)));

		if (double.IsNaN(funFactor) || double.IsInfinity(funFactor))
			return Result.Failure<ScoringProfile>(new Error("Routing.ScoringProfile.NonFinite", "FunFactor must be a finite number."));

		// Pragmatic bound; adjust if your routing math needs a different range.
		if (funFactor is < 0.0 or > 10.0)
			return Result.Failure<ScoringProfile>(RoutingErrors.InvalidFunFactor(funFactor));

		return Result.Success(new ScoringProfile(funFactor, weights));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(ScoringProfile other)
	{
		return FunFactor.Equals(other.FunFactor)
			   && Weights.Equals(other.Weights);
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(FunFactor, Weights);
	}

	#endregion
}