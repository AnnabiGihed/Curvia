using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Users.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed display name for a Curvia user.
///              Sourced from the Keycloak JWT token's preferred_username or name claim
///              during JIT provisioning.  May be updated when the user changes their
///              display name in Keycloak.
///              Replaces the raw <c>string DisplayName</c> (or Username) field on the
///              <c>User</c> aggregate.
///              Persisted as NVARCHAR(100).
/// </summary>
public sealed class DisplayName : CSharpFunctionalExtensions.ValueObject<DisplayName>
{
	#region Constants
	/// <summary>Maximum allowed character length for a display name.</summary>
	public const int MaxLength = 100;
	#endregion

	#region Properties
	/// <summary>The user's display name. Never null, empty, or whitespace-only.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private DisplayName() { Value = default!; }

	/// <summary>Initialises a <see cref="DisplayName"/> with a pre-validated value.</summary>
	/// <param name="value">Validated display name.</param>
	private DisplayName(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="DisplayName"/>.
	/// </summary>
	/// <param name="value">Raw display name. Will be trimmed. Must not be null or whitespace and must not exceed <see cref="MaxLength"/> characters.</param>
	/// <returns>Success containing the <see cref="DisplayName"/>; otherwise failure.</returns>
	public static Result<DisplayName> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<DisplayName>(new Error("DisplayName.Required", "Display name is required."));

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<DisplayName>(new Error("DisplayName.TooLong", $"Display name exceeds the maximum length of {MaxLength} characters."));

		return Result.Success(new DisplayName(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored display name string.</param>
	/// <returns>A <see cref="DisplayName"/> without validation.</returns>
	public static DisplayName FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(DisplayName other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToLowerInvariant().GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}