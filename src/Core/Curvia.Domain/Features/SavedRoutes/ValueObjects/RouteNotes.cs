using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for private rider notes on a saved route.
///              E.g. "Bring rain gear after Bouillon", "Petrol station at Houffalize".
///              Max 1,000 chars, trimmed.
///              Optional on SavedRoute — aggregate holds RouteNotes? (nullable).
///              Never shown publicly regardless of route visibility setting.
/// </summary>
public sealed class RouteNotes : CSharpFunctionalExtensions.ValueObject<RouteNotes>
{
	#region Properties
	/// <summary>
	/// Gets the trimmed notes value.
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private RouteNotes()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="RouteNotes"/> instance.
	/// </summary>
	/// <param name="value">Normalized notes value.</param>
	private RouteNotes(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="RouteNotes"/> after validating:
	/// - Non-empty input
	/// - Maximum length of 1,000 characters
	/// The stored value is trimmed.
	/// </summary>
	/// <param name="value">Raw notes input.</param>
	/// <returns>A successful result containing <see cref="RouteNotes"/>, or a failure with domain error.</returns>
	public static Result<RouteNotes> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<RouteNotes>(new Error("RouteNotes.Empty", "Notes must not be empty."));

		if (value.Length > 1000)
			return Result.Failure<RouteNotes>(new Error("RouteNotes.TooLong", "Notes must not exceed 1,000 characters."));

		return Result.Success(new RouteNotes(value.Trim()));
	}

	/// <summary>
	/// For EF Core materialization only.
	/// Bypasses domain validation — DB values are assumed valid.
	/// </summary>
	/// <param name="value">Persisted notes value.</param>
	/// <returns><see cref="RouteNotes"/> instance.</returns>
	public static RouteNotes FromPersistence(string value)
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
	/// Compares two <see cref="RouteNotes"/> instances using ordinal string comparison.
	/// </summary>
	/// <param name="other">Other notes instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(RouteNotes other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the notes as string.
	/// </summary>
	/// <returns>Notes string.</returns>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}