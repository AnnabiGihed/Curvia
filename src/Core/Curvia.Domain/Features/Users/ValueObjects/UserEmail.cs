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
	public string Value { get; }
	#endregion

	#region Constructors
	private UserEmail() => Value = default!;
	private UserEmail(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<UserEmail> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<UserEmail>(new Error("UserEmail.Empty", "Email must not be empty."));

		if (value.Length > 320)
			return Result.Failure<UserEmail>(new Error("UserEmail.TooLong", "Email must not exceed 320 characters."));

		return Result.Success(new UserEmail(value.Trim().ToLowerInvariant()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static UserEmail FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(UserEmail other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}