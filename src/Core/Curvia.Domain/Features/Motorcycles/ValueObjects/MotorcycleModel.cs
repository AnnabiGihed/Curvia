using Templates.Core.Domain.Shared;

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
	private MotorcycleModel() => Value = default!;
	private MotorcycleModel(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<MotorcycleModel> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<MotorcycleModel>(new Error("MotorcycleModel.Empty", "Model name must not be empty."));

		if (value.Length > 150)
			return Result.Failure<MotorcycleModel>(new Error("MotorcycleModel.TooLong", "Model name must not exceed 150 characters."));

		return Result.Success(new MotorcycleModel(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static MotorcycleModel FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(MotorcycleModel other)
		=> string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	protected override int GetHashCodeCore()
		=> StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}