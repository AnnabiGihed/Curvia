using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.MotorcycleCatalog.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Public model name for a <see cref="Aggregates.MotorcycleCatalogModel"/>
///              (e.g. "Monster 937", "R 1250 GS Adventure").
///              Must not be null or empty.  Max length: 150 characters.
/// </summary>
public sealed class ModelName : CSharpFunctionalExtensions.ValueObject<ModelName>
{
	#region Constants
	/// <summary>Maximum allowed length for a model name.</summary>
	public const int MaxLength = 150;
	#endregion

	#region Properties
	/// <summary>Trimmed model name.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private ModelName() { Value = default!; }

	/// <summary>Initialises a <see cref="ModelName"/> with a validated value.</summary>
	/// <param name="value">Validated, trimmed model name.</param>
	private ModelName(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="ModelName"/>.
	/// </summary>
	/// <param name="value">Raw name input.  Trimmed before storage.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<ModelName> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<ModelName>(new Error("ModelName.Required", "Model name is required."));

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<ModelName>(new Error("ModelName.TooLong", $"Model name must not exceed {MaxLength} characters."));

		return Result.Success(new ModelName(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored model name value.</param>
	/// <returns>A <see cref="ModelName"/> without validation.</returns>
	public static ModelName FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(ModelName other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToUpperInvariant().GetHashCode();
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="ModelName"/> where a <see cref="string"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator string(ModelName vo) => vo.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}