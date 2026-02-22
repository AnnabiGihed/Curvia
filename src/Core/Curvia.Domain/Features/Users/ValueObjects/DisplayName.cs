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
	public string Value { get; }
	#endregion

	#region Constructors
	private DisplayName() => Value = default!;
	private DisplayName(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<DisplayName> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<DisplayName>(new Error("DisplayName.Empty", "Display name must not be empty."));

		if (value.Length > 100)
			return Result.Failure<DisplayName>(new Error("DisplayName.TooLong", "Display name must not exceed 100 characters."));

		return Result.Success(new DisplayName(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static DisplayName FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(DisplayName other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}