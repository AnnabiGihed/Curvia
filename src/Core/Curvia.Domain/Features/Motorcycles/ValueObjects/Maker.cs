using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Motorcycles.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a motorcycle manufacturer name.
///              E.g. "Ducati", "BMW Motorrad", "Honda", "KTM".
///              Max 100 chars, trimmed, non-empty.
/// </summary>
public sealed class Maker : CSharpFunctionalExtensions.ValueObject<Maker>
{
	#region Properties
	public string Value { get; }
	#endregion

	#region Constructors
	private Maker() => Value = default!;
	private Maker(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<Maker> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<Maker>(new Error("Maker.Empty", "Manufacturer name must not be empty."));

		if (value.Length > 100)
			return Result.Failure<Maker>(new Error("Maker.TooLong", "Manufacturer name must not exceed 100 characters."));

		return Result.Success(new Maker(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static Maker FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(Maker other)
		=> string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	protected override int GetHashCodeCore()
		=> StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}