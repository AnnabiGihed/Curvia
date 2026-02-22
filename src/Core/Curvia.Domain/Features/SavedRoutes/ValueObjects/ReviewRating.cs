using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a route review star rating.
///              Valid range: 1–5 (inclusive).
///              1★ = Disappointing   2★ = Below average   3★ = Good
///              4★ = Great           5★ = Outstanding
///              Two ratings are equal iff their integer values are equal.
/// </summary>
public sealed class ReviewRating : CSharpFunctionalExtensions.ValueObject<ReviewRating>
{
	#region Properties
	public int Value { get; }
	#endregion

	#region Constructors
	private ReviewRating() { }
	private ReviewRating(int value) => Value = value;
	#endregion

	#region Factory
	public static Result<ReviewRating> Create(int value)
	{
		if (value is < 1 or > 5)
			return Result.Failure<ReviewRating>(new Error("ReviewRating.OutOfRange", $"Rating must be between 1 and 5. Got {value}."));

		return Result.Success(new ReviewRating(value));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static ReviewRating FromPersistence(int value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(ReviewRating other) => Value == other.Value;
	protected override int GetHashCodeCore() => Value.GetHashCode();
	#endregion

	#region Overrides
	public override string ToString() => $"{Value}★";
	#endregion
}
