using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for the Keycloak "sub" claim.
///              Format: UUID string (e.g. "a3f1c2d4-9e1b-4c7a-bb82-f1234567890a").
///              Max 128 chars — UUID is 36, headroom for future formats.
///              Immutable once set; Keycloak never changes the sub claim.
/// </summary>
public sealed class KeycloakId : CSharpFunctionalExtensions.ValueObject<KeycloakId>
{
	#region Properties
	/// <summary>
	/// Gets the Keycloak subject identifier value.
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private KeycloakId()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="KeycloakId"/> instance.
	/// </summary>
	/// <param name="value">Normalized Keycloak subject value.</param>
	private KeycloakId(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="KeycloakId"/> after validating:
	/// - Non-empty input
	/// - Maximum length of 128 characters
	/// The value is trimmed but otherwise not transformed.
	/// </summary>
	/// <param name="value">Raw Keycloak subject value.</param>
	/// <returns>A successful result containing <see cref="KeycloakId"/>, or a failure with domain error.</returns>
	public static Result<KeycloakId> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<KeycloakId>(new Error("KeycloakId.Empty", "KeycloakId must not be empty."));

		if (value.Length > 128)
			return Result.Failure<KeycloakId>(new Error("KeycloakId.TooLong", "KeycloakId must not exceed 128 characters."));

		return Result.Success(new KeycloakId(value.Trim()));
	}

	/// <summary>
	/// Rehydrates a <see cref="KeycloakId"/> from a persisted value without re-validation.
	/// Intended for EF Core materialization.
	/// </summary>
	/// <param name="value">Persisted Keycloak subject value.</param>
	/// <returns><see cref="KeycloakId"/> instance.</returns>
	public static KeycloakId FromPersistence(string value)
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
	/// Compares two <see cref="KeycloakId"/> instances using ordinal string comparison.
	/// </summary>
	/// <param name="other">Other KeycloakId instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(KeycloakId other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the Keycloak subject identifier as string.
	/// </summary>
	/// <returns>KeycloakId string.</returns>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}