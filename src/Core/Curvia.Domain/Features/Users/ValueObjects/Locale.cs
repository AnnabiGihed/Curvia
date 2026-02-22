using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a BCP-47 locale tag (e.g. "fr-BE", "nl-BE", "en-US").
///              Max 10 chars — longest common BCP-47 tags are 5 chars; 10 gives safe headroom.
///              Used for UI localisation. Stored as-is; no format enforcement beyond length.
///              Optional on User — aggregate holds Locale? (nullable).
/// </summary>
public sealed class Locale : CSharpFunctionalExtensions.ValueObject<Locale>
{
	#region Properties
	public string Value { get; }
	#endregion

	#region Constructors
	private Locale() => Value = default!;
	private Locale(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<Locale> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<Locale>(new Error("Locale.Empty", "Locale must not be empty."));

		if (value.Length > 10)
			return Result.Failure<Locale>(new Error("Locale.TooLong", "Locale must not exceed 10 characters."));

		return Result.Success(new Locale(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static Locale FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(Locale other)
		=> string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	protected override int GetHashCodeCore()
		=> StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}