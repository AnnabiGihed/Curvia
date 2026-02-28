using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed wrapper around the Keycloak subject claim ("sub") for a user.
///              The subject is the stable, immutable UUID-string that Keycloak assigns to
///              each user account.  It is the primary link between the Curvia AppUser record
///              and the Keycloak identity provider.
///
///              Replaces the raw <c>string KeycloakId</c> field on the <c>User</c> aggregate
///              to prevent null, empty, or clearly non-UUID values from entering the domain.
///              Persisted as NVARCHAR(36) (UUID canonical form: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
/// </summary>
public sealed class KeycloakSubject : CSharpFunctionalExtensions.ValueObject<KeycloakSubject>
{
	#region Constants
	/// <summary>Maximum column length — UUID string (36 chars + buffer for edge cases).</summary>
	public const int MaxLength = 36;
	#endregion

	#region Properties
	/// <summary>Keycloak subject string (UUID format, e.g. "3fa85f64-5717-4562-b3fc-2c963f66afa6").</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private KeycloakSubject() { Value = default!; }

	/// <summary>Initialises a <see cref="KeycloakSubject"/> with a pre-validated value.</summary>
	/// <param name="value">Validated Keycloak subject string.</param>
	private KeycloakSubject(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="KeycloakSubject"/>.
	/// </summary>
	/// <param name="value">Raw Keycloak subject string. Must not be null or whitespace, and must be a valid UUID.</param>
	/// <returns>Success containing the <see cref="KeycloakSubject"/>; otherwise failure.</returns>
	public static Result<KeycloakSubject> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<KeycloakSubject>(new Error("KeycloakSubject.Required", "Keycloak subject is required."));

		var trimmed = value.Trim();

		if (!Guid.TryParse(trimmed, out _))
			return Result.Failure<KeycloakSubject>(new Error("KeycloakSubject.InvalidFormat", $"'{trimmed}' is not a valid Keycloak subject UUID."));

		return Result.Success(new KeycloakSubject(trimmed.ToLowerInvariant()));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored Keycloak subject string.</param>
	/// <returns>A <see cref="KeycloakSubject"/> without validation.</returns>
	public static KeycloakSubject FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(KeycloakSubject other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToLowerInvariant().GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}