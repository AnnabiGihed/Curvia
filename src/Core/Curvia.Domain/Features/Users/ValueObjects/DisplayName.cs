using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a user's public display name.
///              Sourced from the Keycloak "preferred_username" claim on first login.
///              User may override via profile settings.
///              Max 100 chars, trimmed, non-empty.
/// </summary>
public sealed class DisplayName : CSharpFunctionalExtensions.ValueObject<DisplayName>
{
	#region Properties
	/// <summary>
	/// Gets the trimmed display name value.
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private DisplayName()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="DisplayName"/> instance.
	/// </summary>
	/// <param name="value">Normalized display name value.</param>
	private DisplayName(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="DisplayName"/> after validating:
	/// - Non-empty input
	/// - Maximum length of 100 characters
	/// The stored value is trimmed.
	/// </summary>
	/// <param name="value">Raw display name input.</param>
	/// <returns>A successful result containing <see cref="DisplayName"/>, or a failure with domain error.</returns>
	public static Result<DisplayName> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<DisplayName>(new Error("DisplayName.Empty", "Display name must not be empty."));

		if (value.Length > 100)
			return Result.Failure<DisplayName>(new Error("DisplayName.TooLong", "Display name must not exceed 100 characters."));

		return Result.Success(new DisplayName(value.Trim()));
	}

	/// <summary>
	/// For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.
	/// </summary>
	/// <param name="value">Persisted display name value.</param>
	/// <returns><see cref="DisplayName"/> instance.</returns>
	public static DisplayName FromPersistence(string value)
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
	/// Compares two <see cref="DisplayName"/> instances using ordinal string comparison.
	/// </summary>
	/// <param name="other">Other display name instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(DisplayName other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the display name as string.
	/// </summary>
	/// <returns>Display name string.</returns>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}