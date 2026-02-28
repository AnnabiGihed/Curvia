using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Routing.Shared.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Multiplier that limits how far the generated route may deviate from the
///              straight-line (direct) distance between start and end points.
///              A value of 1.0 means "no detour allowed"; 2.0 means "route may be up to
///              twice the direct distance".
///
///              Enforced range: 1.0 – 4.0 (hard cap prevents unbounded detours that would
///              make routing impractical and expose Valhalla to runaway computation).
///              Replaces <c>double MaxDetourRatio</c> in <c>RoutingConstraints</c>.
/// </summary>
public sealed class MaxDetourRatio : CSharpFunctionalExtensions.ValueObject<MaxDetourRatio>
{
	#region Constants
	/// <summary>Minimum allowed ratio — no detour.</summary>
	public const double MinValue = 1.0;

	/// <summary>Maximum allowed ratio — four times the direct distance.</summary>
	public const double MaxValue = 4.0;
	#endregion

	#region Properties
	/// <summary>Detour ratio. Range: 1.0 – 4.0.</summary>
	public double Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private MaxDetourRatio() { }

	/// <summary>Initialises a <see cref="MaxDetourRatio"/> with a pre-validated value.</summary>
	/// <param name="value">Validated ratio.</param>
	private MaxDetourRatio(double value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="MaxDetourRatio"/>.
	/// </summary>
	/// <param name="value">Raw detour ratio. Must be between 1.0 and 4.0 inclusive.</param>
	/// <returns>Success containing the <see cref="MaxDetourRatio"/>; otherwise failure.</returns>
	public static Result<MaxDetourRatio> Create(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			return Result.Failure<MaxDetourRatio>(new Error("MaxDetourRatio.Invalid", $"Detour ratio '{value}' is not a finite number."));

		if (value < MinValue || value > MaxValue)
			return Result.Failure<MaxDetourRatio>(new Error("MaxDetourRatio.OutOfRange", $"Detour ratio '{value}' must be between {MinValue} and {MaxValue}."));

		return Result.Success(new MaxDetourRatio(value));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored ratio value.</param>
	/// <returns>A <see cref="MaxDetourRatio"/> without validation.</returns>
	public static MaxDetourRatio FromPersistence(double value) => new(value);

	/// <summary>Default ratio of 1.5 — a sensible starting point for most route plans.</summary>
	public static MaxDetourRatio Default => new(1.5);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(MaxDetourRatio other) => Math.Abs(Value - other.Value) < 1e-6;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"×{Value:F2}";
	#endregion
}
