using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Defines normalized scoring weights for route fun calculation.
///              Each weight is within [0, 1]. At least one must be > 0.
/// </summary>
public sealed class ScoringWeights : CSharpFunctionalExtensions.ValueObject<ScoringWeights>
{
	#region Properties

	public double Curves { get; }
	public double Elevation { get; }
	public double Scenery { get; }

	#endregion

	#region Constructor

	private ScoringWeights(double curves, double elevation, double scenery)
	{
		Curves = curves;
		Elevation = elevation;
		Scenery = scenery;
	}

	#endregion

	#region Factory

	public static Result<ScoringWeights> Create(double curves, double elevation, double scenery)
	{
		if (!IsFinite(curves))
			return Result.Failure<ScoringWeights>(RoutingErrors.NonFiniteNumber(nameof(curves)));
		if (!IsFinite(elevation))
			return Result.Failure<ScoringWeights>(RoutingErrors.NonFiniteNumber(nameof(elevation)));
		if (!IsFinite(scenery))
			return Result.Failure<ScoringWeights>(RoutingErrors.NonFiniteNumber(nameof(scenery)));

		if (curves is < 0.0 or > 1.0)
			return Result.Failure<ScoringWeights>(RoutingErrors.WeightOutOfRange(nameof(curves), curves));
		if (elevation is < 0.0 or > 1.0)
			return Result.Failure<ScoringWeights>(RoutingErrors.WeightOutOfRange(nameof(elevation), elevation));
		if (scenery is < 0.0 or > 1.0)
			return Result.Failure<ScoringWeights>(RoutingErrors.WeightOutOfRange(nameof(scenery), scenery));

		if (curves <= 0 && elevation <= 0 && scenery <= 0)
			return Result.Failure<ScoringWeights>(RoutingErrors.WeightsAllZero());

		return Result.Success(new ScoringWeights(curves, elevation, scenery));
	}

	private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

	#endregion

	#region Equality

	protected override bool EqualsCore(ScoringWeights other)
		=> Curves.Equals(other.Curves)
		   && Elevation.Equals(other.Elevation)
		   && Scenery.Equals(other.Scenery);

	protected override int GetHashCodeCore()
		=> HashCode.Combine(Curves, Elevation, Scenery);

	#endregion

	#region Overrides

	public override string ToString()
		=> $"Curves={Curves:0.##}, Elevation={Elevation:0.##}, Scenery={Scenery:0.##}";

	#endregion
}
