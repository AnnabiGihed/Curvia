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
	/// <summary>
	/// Gets the trimmed route name value.
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private RouteName()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="RouteName"/> instance.
	/// </summary>
	/// <param name="value">Normalized route name value.</param>
	private RouteName(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="RouteName"/> after validating:
	/// - Non-empty input
	/// - Maximum length of 200 characters
	/// The stored value is trimmed.
	/// </summary>
	/// <param name="value">Raw route name input.</param>
	/// <returns>A successful result containing <see cref="RouteName"/>, or a failure with domain error.</returns>
	public static Result<RouteName> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<RouteName>(new Error("RouteName.Empty", "Route name must not be empty."));

		if (value.Length > 200)
			return Result.Failure<RouteName>(new Error("RouteName.TooLong", "Route name must not exceed 200 characters."));

		return Result.Success(new RouteName(value.Trim()));
	}

	/// <summary>
	/// For EF Core materialization only.
	/// Bypasses domain validation — DB values are assumed valid.
	/// </summary>
	/// <param name="value">Persisted route name value.</param>
	/// <returns><see cref="RouteName"/> instance.</returns>
	public static RouteName FromPersistence(string value)
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
	/// Compares two <see cref="RouteName"/> instances using ordinal string comparison.
	/// </summary>
	/// <param name="other">Other route name instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(RouteName other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the route name as string.
	/// </summary>
	/// <returns>Route name string.</returns>
	public override string ToString() => Value;
	#endregion
}