using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Enforced speed limit in km/h for a <see cref="Aggregates.SpeedCamera"/>.
///              Valid range: 1 to 300 km/h.
///              Use the nullable overload pattern: the camera record as a whole is optional,
///              so the aggregate holds <c>SpeedLimit?</c> to represent an unknown limit.
/// </summary>
public sealed class SpeedLimit : CSharpFunctionalExtensions.ValueObject<SpeedLimit>
{
	#region Constants
	/// <summary>Minimum valid speed limit in km/h.</summary>
	public const int MinKmh = 1;

	/// <summary>Maximum valid speed limit in km/h.</summary>
	public const int MaxKmh = 300;
	#endregion

	#region Properties
	/// <summary>Speed limit value in kilometres per hour.</summary>
	public int Kmh { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private SpeedLimit() { }

	/// <summary>Initialises a <see cref="SpeedLimit"/> with a validated value.</summary>
	/// <param name="kmh">Validated speed in km/h.</param>
	private SpeedLimit(int kmh) => Kmh = kmh;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="SpeedLimit"/>.
	/// </summary>
	/// <param name="kmh">Speed limit in km/h.  Must be between 1 and 300 inclusive.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<SpeedLimit> Create(int kmh)
	{
		if (kmh < MinKmh || kmh > MaxKmh)
			return Result.Failure<SpeedLimit>(new Error("SpeedLimit.OutOfRange", $"Speed limit '{kmh}' km/h is outside the valid range ({MinKmh}–{MaxKmh})."));

		return Result.Success(new SpeedLimit(kmh));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="kmh">Stored speed limit value.</param>
	/// <returns>A <see cref="SpeedLimit"/> without validation.</returns>
	public static SpeedLimit FromPersistence(int kmh) => new(kmh);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(SpeedLimit other) => Kmh == other.Kmh;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Kmh;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"{Kmh} km/h";
	#endregion
}