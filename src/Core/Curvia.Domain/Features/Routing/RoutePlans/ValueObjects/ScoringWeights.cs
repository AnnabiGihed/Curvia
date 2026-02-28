using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;
using Curvia.Domain.Features.Routing.Shared.ValueObjects;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Normalised scoring weights for the three fun-routing dimensions: curves, elevation, scenery.
///              Each weight is wrapped in a <see cref="ScoringWeight"/> value object to enforce the
///              [0.0, 1.0] range invariant at construction time rather than relying on caller discipline.
///              At least one weight must be greater than zero — an all-zero profile produces no useful routing.
///              The <see cref="Curves"/>, <see cref="Elevation"/>, and <see cref="Scenery"/> double
///              accessors provide backward-compatible surface for the scoring pipeline without requiring it
///              to unwrap the inner VOs on every call.
/// </summary>
public sealed class ScoringWeights : CSharpFunctionalExtensions.ValueObject<ScoringWeights>
{
	#region Properties
	/// <summary>Strongly-typed curvature weight. Range: 0.0 – 1.0.</summary>
	public ScoringWeight CurvesWeight { get; }

	/// <summary>Strongly-typed elevation and relief weight. Range: 0.0 – 1.0.</summary>
	public ScoringWeight ElevationWeight { get; }

	/// <summary>Strongly-typed scenery weight. Range: 0.0 – 1.0.</summary>
	public ScoringWeight SceneryWeight { get; }

	/// <summary>Raw curvature weight value. Convenience accessor for the scoring pipeline.</summary>
	public double Curves => CurvesWeight.Value;

	/// <summary>Raw elevation weight value. Convenience accessor for the scoring pipeline.</summary>
	public double Elevation => ElevationWeight.Value;

	/// <summary>Raw scenery weight value. Convenience accessor for the scoring pipeline.</summary>
	public double Scenery => SceneryWeight.Value;
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private ScoringWeights() { CurvesWeight = default!; ElevationWeight = default!; SceneryWeight = default!; }

	/// <summary>Initialises a <see cref="ScoringWeights"/> with pre-validated weight value objects.</summary>
	/// <param name="curvesWeight">Validated curvature weight VO.</param>
	/// <param name="elevationWeight">Validated elevation weight VO.</param>
	/// <param name="sceneryWeight">Validated scenery weight VO.</param>
	private ScoringWeights(ScoringWeight curvesWeight, ScoringWeight elevationWeight, ScoringWeight sceneryWeight)
	{
		CurvesWeight = curvesWeight;
		ElevationWeight = elevationWeight;
		SceneryWeight = sceneryWeight;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="ScoringWeights"/> from raw scalar inputs.
	/// Each dimension is validated through <see cref="ScoringWeight.Create"/>.
	/// At least one weight must be strictly greater than zero.
	/// </summary>
	/// <param name="curves">Curvature weight. Range: 0.0 – 1.0.</param>
	/// <param name="elevation">Elevation weight. Range: 0.0 – 1.0.</param>
	/// <param name="scenery">Scenery weight. Range: 0.0 – 1.0.</param>
	/// <returns>Success containing the <see cref="ScoringWeights"/>; otherwise failure.</returns>
	public static Result<ScoringWeights> Create(double curves, double elevation, double scenery)
	{
		var curvesResult = ScoringWeight.Create(curves);
		if (curvesResult.IsFailure)
			return Result.Failure<ScoringWeights>(curvesResult.Error);

		var elevationResult = ScoringWeight.Create(elevation);
		if (elevationResult.IsFailure)
			return Result.Failure<ScoringWeights>(elevationResult.Error);

		var sceneryResult = ScoringWeight.Create(scenery);
		if (sceneryResult.IsFailure)
			return Result.Failure<ScoringWeights>(sceneryResult.Error);

		if (curves <= 0.0 && elevation <= 0.0 && scenery <= 0.0)
			return Result.Failure<ScoringWeights>(RoutingErrors.WeightsAllZero());

		return Result.Success(new ScoringWeights(curvesResult.Value, elevationResult.Value, sceneryResult.Value));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="curves">Stored curvature weight value.</param>
	/// <param name="elevation">Stored elevation weight value.</param>
	/// <param name="scenery">Stored scenery weight value.</param>
	/// <returns>A <see cref="ScoringWeights"/> without validation.</returns>
	public static ScoringWeights FromPersistence(double curves, double elevation, double scenery) =>
		new(ScoringWeight.FromPersistence(curves), ScoringWeight.FromPersistence(elevation), ScoringWeight.FromPersistence(scenery));
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(ScoringWeights other) =>
		CurvesWeight.Equals(other.CurvesWeight) && ElevationWeight.Equals(other.ElevationWeight) && SceneryWeight.Equals(other.SceneryWeight);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => HashCode.Combine(CurvesWeight, ElevationWeight, SceneryWeight);
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"Curves={Curves:0.##}, Elevation={Elevation:0.##}, Scenery={Scenery:0.##}";
	#endregion
}