using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Motorcycles.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Engine displacement in cubic centimetres for a <see cref="Aggregates.Motorcycle"/>.
///              Valid range: 50 cc to 2 500 cc (covers moped to top-of-range large displacement).
///              Held as optional on the aggregate — not all users specify engine size.
/// </summary>
public sealed class EngineDisplacement : CSharpFunctionalExtensions.ValueObject<EngineDisplacement>
{
	#region Constants
	/// <summary>Minimum valid displacement in cc.</summary>
	public const int MinCc = 50;

	/// <summary>Maximum valid displacement in cc.</summary>
	public const int MaxCc = 2_500;
	#endregion

	#region Properties
	/// <summary>Engine displacement in cubic centimetres.</summary>
	public int Cc { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private EngineDisplacement() { }

	/// <summary>Initialises an <see cref="EngineDisplacement"/> with a validated value.</summary>
	/// <param name="cc">Validated displacement in cc.</param>
	private EngineDisplacement(int cc) => Cc = cc;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="EngineDisplacement"/>.
	/// </summary>
	/// <param name="cc">Engine displacement in cc.  Must be between 50 and 2 500.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<EngineDisplacement> Create(int cc)
	{
		if (cc < MinCc || cc > MaxCc)
			return Result.Failure<EngineDisplacement>(new Error("EngineDisplacement.OutOfRange", $"Engine displacement '{cc}' cc must be between {MinCc} and {MaxCc}."));

		return Result.Success(new EngineDisplacement(cc));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="cc">Stored displacement value.</param>
	/// <returns>An <see cref="EngineDisplacement"/> without validation.</returns>
	public static EngineDisplacement FromPersistence(int cc) => new(cc);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(EngineDisplacement other) => Cc == other.Cc;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Cc;
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="EngineDisplacement"/> where an <see cref="int"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator int(EngineDisplacement vo) => vo.Cc;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"{Cc} cc";
	#endregion
}