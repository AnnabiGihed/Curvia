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
	public string Value { get; }
	#endregion

	#region Constructors
	private RouteNotes() => Value = default!;
	private RouteNotes(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<RouteNotes> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<RouteNotes>(new Error("RouteNotes.Empty", "Notes must not be empty."));

		if (value.Length > 1000)
			return Result.Failure<RouteNotes>(new Error("RouteNotes.TooLong", "Notes must not exceed 1,000 characters."));

		return Result.Success(new RouteNotes(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static RouteNotes FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(RouteNotes other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}