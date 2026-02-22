using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a user's email address.
///              Enforces RFC 5321 max length (320 chars), non-empty, and stores lowercase.
///              Two emails are equal iff their lowercased values are equal.
/// </summary>
public sealed class UserEmail : CSharpFunctionalExtensions.ValueObject<UserEmail>
{
	#region Properties
	/// <summary>
	/// Gets the normalized (trimmed + lowercased) email value.
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private UserEmail()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="UserEmail"/> with a normalized value.
	/// </summary>
	/// <param name="value">Normalized email value.</param>
	private UserEmail(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="UserEmail"/> after validating:
	/// - Non-empty input
	/// - Maximum length of 320 characters (RFC 5321)
	/// The stored value is trimmed and converted to lowercase invariant.
	/// </summary>
	/// <param name="value">Raw email input.</param>
	/// <returns>A successful result containing <see cref="UserEmail"/>, or a failure with domain error.</returns>
	public static Result<UserEmail> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<UserEmail>(new Error("UserEmail.Empty", "Email must not be empty."));

		if (value.Length > 320)
			return Result.Failure<UserEmail>(new Error("UserEmail.TooLong", "Email must not exceed 320 characters."));

		return Result.Success(new UserEmail(value.Trim().ToLowerInvariant()));
	}

	/// <summary>
	/// Rehydrates a <see cref="UserEmail"/> from a persisted value without re-validation.
	/// Intended for EF Core materialization.
	/// </summary>
	/// <param name="value">Persisted email value.</param>
	/// <returns><see cref="UserEmail"/> instance.</returns>
	public static UserEmail FromPersistence(string value)
	{
		return new(value);
	}
	#endregion

	#region Equality
	/// <summary>
	/// Computes hash code using ordinal string comparison.
	/// </summary>
	protected override int GetHashCodeCore()
	{
		return StringComparer.Ordinal.GetHashCode(Value);
	}

	/// <summary>
	/// Compares two <see cref="UserEmail"/> instances using ordinal equality on the normalized value.
	/// </summary>
	/// <param name="other">Other email instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(UserEmail other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the normalized email string.
	/// </summary>
	/// <returns>Email as string.</returns>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}