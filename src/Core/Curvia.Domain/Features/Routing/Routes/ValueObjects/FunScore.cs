using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a normalized fun score in range [0, 1].
///              Used to score segments/routes (curves, elevation, scenery, etc.).
/// </summary>
public sealed class FunScore : CSharpFunctionalExtensions.ValueObject<FunScore>
{
	#region Properties

	public double Value { get; }

	#endregion

	#region Constructor
	private FunScore()
	{
	}
	private FunScore(double value)
	{
		Value = value;
	}

	#endregion

	#region Factory

	public static Result<FunScore> Create(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			return Result.Failure<FunScore>(RoutingErrors.NonFiniteNumber(nameof(value)));

		if (value is < 0.0 or > 1.0)
			return Result.Failure<FunScore>(RoutingErrors.FunScoreOutOfRange(value));

		return Result.Success(new FunScore(value));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(FunScore other)
		=> Value.Equals(other.Value);

	protected override int GetHashCodeCore()
		=> Value.GetHashCode();

	#endregion

	#region Overrides

	public override string ToString() => $"{Value:0.###}";

	#endregion
}
