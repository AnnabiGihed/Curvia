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
	public string Value { get; }
	#endregion

	#region Constructors
	private KeycloakId() => Value = default!;
	private KeycloakId(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<KeycloakId> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<KeycloakId>(new Error("KeycloakId.Empty", "KeycloakId must not be empty."));

		if (value.Length > 128)
			return Result.Failure<KeycloakId>(new Error("KeycloakId.TooLong", "KeycloakId must not exceed 128 characters."));

		return Result.Success(new KeycloakId(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static KeycloakId FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(KeycloakId other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}