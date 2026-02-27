using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed e-mail address for a Curvia user.
///              Sourced from the Keycloak JWT token during JIT provisioning and updated
///              on subsequent logins if the user has changed their e-mail in Keycloak.
///              Replaces the raw <c>string Email</c> field on the <c>User</c> aggregate.
///              Validation uses a simple structural check (@ sign + domain part) — full
///              RFC 5321 compliance is delegated to Keycloak's registration flow.
///              Persisted as NVARCHAR(320) — the RFC 5321 maximum e-mail address length.
/// </summary>
public sealed class UserEmail : CSharpFunctionalExtensions.ValueObject<UserEmail>
{
	#region Constants
	/// <summary>Maximum column length per RFC 5321 (local + @ + domain).</summary>
	public const int MaxLength = 320;
	#endregion

	#region Properties
	/// <summary>Lowercase normalised e-mail address string.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private UserEmail() { Value = default!; }

	/// <summary>Initialises a <see cref="UserEmail"/> with a pre-validated value.</summary>
	/// <param name="value">Validated, normalised e-mail address.</param>
	private UserEmail(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="UserEmail"/>.
	/// </summary>
	/// <param name="value">Raw e-mail input. Will be trimmed and lowercased. Must contain an @ sign and a non-empty domain part.</param>
	/// <returns>Success containing the <see cref="UserEmail"/>; otherwise failure.</returns>
	public static Result<UserEmail> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<UserEmail>(new Error("UserEmail.Required", "E-mail address is required."));

		var normalised = value.Trim().ToLowerInvariant();

		if (normalised.Length > MaxLength)
			return Result.Failure<UserEmail>(new Error("UserEmail.TooLong", $"E-mail address exceeds the RFC 5321 maximum length of {MaxLength} characters."));

		var atIndex = normalised.IndexOf('@');
		if (atIndex <= 0 || atIndex == normalised.Length - 1 || normalised.IndexOf('.', atIndex) < 0)
			return Result.Failure<UserEmail>(new Error("UserEmail.InvalidFormat", $"'{normalised}' is not a valid e-mail address."));

		return Result.Success(new UserEmail(normalised));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored e-mail address string.</param>
	/// <returns>A <see cref="UserEmail"/> without validation.</returns>
	public static UserEmail FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(UserEmail other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}