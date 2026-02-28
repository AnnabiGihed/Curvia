using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Routing.Shared.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Normalised scoring weight in the range [0.0, 1.0] used within
///              <c>ScoringProfile</c> to express the relative importance of
///              <c>CurvesWeight</c>, <c>ElevationWeight</c>, and <c>SceneryWeight</c>.
///
///              A weight of 0.0 means the dimension is ignored completely; 1.0 means
///              maximum priority.  The three weights do not need to sum to 1.0 — they
///              are independent multipliers applied by the scoring pipeline before the
///              final fun score is normalised.
///
///              Replaces raw <c>double</c> fields in <c>ScoringProfile</c>.
/// </summary>
public sealed class ScoringWeight : CSharpFunctionalExtensions.ValueObject<ScoringWeight>
{
	#region Constants
	/// <summary>Minimum valid weight (inclusive): dimension is completely ignored.</summary>
	public const double Min = 0.0;

	/// <summary>Maximum valid weight (inclusive): dimension is at full priority.</summary>
	public const double Max = 1.0;
	#endregion

	#region Properties
	/// <summary>Normalised weight value. Range: 0.0 – 1.0.</summary>
	public double Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private ScoringWeight() { }

	/// <summary>Initialises a <see cref="ScoringWeight"/> with a pre-validated value.</summary>
	/// <param name="value">Validated weight.</param>
	private ScoringWeight(double value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="ScoringWeight"/>.
	/// </summary>
	/// <param name="value">Raw weight value. Must be between 0.0 and 1.0 inclusive.</param>
	/// <returns>Success containing the <see cref="ScoringWeight"/>; otherwise failure.</returns>
	public static Result<ScoringWeight> Create(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			return Result.Failure<ScoringWeight>(new Error("ScoringWeight.Invalid", $"Weight '{value}' is not a finite number."));

		if (value < Min || value > Max)
			return Result.Failure<ScoringWeight>(new Error("ScoringWeight.OutOfRange", $"Weight '{value}' must be between {Min} and {Max}."));

		return Result.Success(new ScoringWeight(value));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored weight value.</param>
	/// <returns>A <see cref="ScoringWeight"/> without validation.</returns>
	public static ScoringWeight FromPersistence(double value) => new(value);

	/// <summary>Full weight (1.0) — convenience factory for "maximum priority".</summary>
	public static ScoringWeight Full => new(1.0);

	/// <summary>Half weight (0.5) — convenience factory for balanced scoring.</summary>
	public static ScoringWeight Half => new(0.5);

	/// <summary>Zero weight (0.0) — convenience factory for "ignore this dimension".</summary>
	public static ScoringWeight Off => new(0.0);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(ScoringWeight other) => Math.Abs(Value - other.Value) < 1e-9;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"{Value:P0}";
	#endregion
}
