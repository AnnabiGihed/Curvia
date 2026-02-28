using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Motorcycles.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a motorcycle model name.
///              E.g. "Monster 937", "R 1250 GS Adventure", "CB650R".
///              Max 150 chars, trimmed, non-empty.
///              Named MotorcycleModel to avoid collision with EF Core's "Model" conventions.
/// </summary>
public sealed class MotorcycleModel : CSharpFunctionalExtensions.ValueObject<MotorcycleModel>
{
	#region Properties
	public string Value { get; }
	#endregion

	#region Constructors
	private MotorcycleModel()
	{
		Value = default!;
	}

	/// <summary>
	/// Initializes a new <see cref="MotorcycleModel"/> value object.
	/// </summary>
	/// <param name="value">Validated model name.</param>
	private MotorcycleModel(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="MotorcycleModel"/> after enforcing domain rules
	/// (non-empty, max 150 characters, trimmed).
	/// </summary>
	/// <param name="value">Raw model name string.</param>
	/// <returns>Successful result containing <see cref="MotorcycleModel"/> or failure with domain error.</returns>
	public static Result<MotorcycleModel> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<MotorcycleModel>(new Error("MotorcycleModel.Empty", "Model name must not be empty."));

		if (value.Length > 150)
			return Result.Failure<MotorcycleModel>(new Error("MotorcycleModel.TooLong", "Model name must not exceed 150 characters."));

		return Result.Success(new MotorcycleModel(value.Trim()));
	}

	/// <summary>
	/// For EF Core materialization only.
	/// Bypasses domain validation — DB values are assumed valid.
	/// </summary>
	public static MotorcycleModel FromPersistence(string value)
	{
		return new(value);
	}
	#endregion

	#region Equality
	protected override bool EqualsCore(MotorcycleModel other)
	{
		return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
	}

	protected override int GetHashCodeCore()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
	}
	#endregion

	#region Overrides
	public override string ToString()
	{
		return Value;
	}
	#endregion
}