using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a user-given saved route name.
///              E.g. "Weekend Ardennes loop", "Semois valley blast".
///              Max 200 chars, trimmed, non-empty.
/// </summary>
public sealed class RouteName : CSharpFunctionalExtensions.ValueObject<RouteName>
{
	#region Properties
	public string Value { get; }
	#endregion

	#region Constructors
	private RouteName() => Value = default!;
	private RouteName(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<RouteName> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<RouteName>(new Error("RouteName.Empty", "Route name must not be empty."));

		if (value.Length > 200)
			return Result.Failure<RouteName>(new Error("RouteName.TooLong", "Route name must not exceed 200 characters."));

		return Result.Success(new RouteName(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static RouteName FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(RouteName other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}