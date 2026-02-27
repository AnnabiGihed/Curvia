using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.MotorcycleCatalog.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Public manufacturer name for a <see cref="Aggregates.MotorcycleMaker"/>
///              (e.g. "Ducati", "BMW Motorrad").
///              Must not be null or empty.  Max length: 100 characters.
/// </summary>
public sealed class MakerName : CSharpFunctionalExtensions.ValueObject<MakerName>
{
	#region Constants
	/// <summary>Maximum allowed length for a maker name.</summary>
	public const int MaxLength = 100;
	#endregion

	#region Properties
	/// <summary>Trimmed manufacturer name.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private MakerName() { Value = default!; }

	/// <summary>Initialises a <see cref="MakerName"/> with a validated value.</summary>
	/// <param name="value">Validated, trimmed manufacturer name.</param>
	private MakerName(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="MakerName"/>.
	/// </summary>
	/// <param name="value">Raw name input.  Trimmed before storage.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<MakerName> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<MakerName>(new Error("MakerName.Required", "Maker name is required."));

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<MakerName>(new Error("MakerName.TooLong", $"Maker name must not exceed {MaxLength} characters."));

		return Result.Success(new MakerName(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored maker name value.</param>
	/// <returns>A <see cref="MakerName"/> without validation.</returns>
	public static MakerName FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(MakerName other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToUpperInvariant().GetHashCode();
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="MakerName"/> where a <see cref="string"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator string(MakerName vo) => vo.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}