using Pivot.Framework.Domain.Shared;

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
	/// <summary>
	/// Gets the locale tag value (trimmed, case preserved).
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private Locale()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="Locale"/> instance.
	/// </summary>
	/// <param name="value">Locale tag value.</param>
	private Locale(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="Locale"/> after validating:
	/// - Non-empty input
	/// - Maximum length of 10 characters
	/// No strict BCP-47 format validation is enforced at this level.
	/// </summary>
	/// <param name="value">Raw locale input.</param>
	/// <returns>A successful result containing <see cref="Locale"/>, or a failure with domain error.</returns>
	public static Result<Locale> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<Locale>(new Error("Locale.Empty", "Locale must not be empty."));

		if (value.Length > 10)
			return Result.Failure<Locale>(new Error("Locale.TooLong", "Locale must not exceed 10 characters."));

		return Result.Success(new Locale(value.Trim()));
	}

	/// <summary>
	/// Rehydrates a <see cref="Locale"/> from a persisted value without re-validation.
	/// Intended for EF Core materialization.
	/// </summary>
	/// <param name="value">Persisted locale value.</param>
	/// <returns><see cref="Locale"/> instance.</returns>
	public static Locale FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <summary>
	/// Computes hash code using case-insensitive ordinal comparison.
	/// </summary>
	protected override int GetHashCodeCore()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
	}

	/// <summary>
	/// Compares two <see cref="Locale"/> instances using case-insensitive ordinal comparison.
	/// </summary>
	/// <param name="other">Other locale instance.</param>
	/// <returns>True if values are equal (case-insensitive); otherwise false.</returns>
	protected override bool EqualsCore(Locale other)
	{
		return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the locale tag as string.
	/// </summary>
	/// <returns>Locale string.</returns>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}