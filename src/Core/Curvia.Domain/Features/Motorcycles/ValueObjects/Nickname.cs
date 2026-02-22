using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Motorcycles.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a user-given motorcycle nickname.
///              E.g. "The Beast", "Mia", "Big Blue".
///              Max 100 chars, trimmed.
///              Optional on Motorcycle — aggregate holds Nickname? (nullable).
/// </summary>
public sealed class Nickname : CSharpFunctionalExtensions.ValueObject<Nickname>
{
	#region Properties
	public string Value { get; }
	#endregion

	#region Constructors
	private Nickname() => Value = default!;
	private Nickname(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<Nickname> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<Nickname>(new Error("Nickname.Empty", "Nickname must not be empty."));

		if (value.Length > 100)
			return Result.Failure<Nickname>(new Error("Nickname.TooLong", "Nickname must not exceed 100 characters."));

		return Result.Success(new Nickname(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static Nickname FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(Nickname other)
		=> string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	protected override int GetHashCodeCore()
		=> StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}