using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a route review star rating.
/// </summary>
public sealed class ReviewRating : CSharpFunctionalExtensions.ValueObject<ReviewRating>
{
	#region Properties
	/// <summary>
	/// Gets the numeric star rating (1–5).
	/// </summary>
	public int Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by some ORMs.
	/// </summary>
	private ReviewRating() { }

	/// <summary>
	/// Creates a new <see cref="ReviewRating"/> instance.
	/// </summary>
	/// <param name="value">Validated rating value.</param>
	private ReviewRating(int value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="ReviewRating"/> after validating that
	/// the rating is between 1 and 5 (inclusive).
	/// </summary>
	/// <param name="value">Raw rating value.</param>
	/// <returns>A successful result containing <see cref="ReviewRating"/>, or a failure with domain error.</returns>
	public static Result<ReviewRating> Create(int value)
	{
		if (value is < 1 or > 5)
			return Result.Failure<ReviewRating>(new Error("ReviewRating.OutOfRange", $"Rating must be between 1 and 5. Got {value}."));

		return Result.Success(new ReviewRating(value));
	}

	/// <summary>
	/// For EF Core materialization only.
	/// Bypasses domain validation — DB values are assumed valid.
	/// </summary>
	/// <param name="value">Persisted rating value.</param>
	/// <returns><see cref="ReviewRating"/> instance.</returns>
	public static ReviewRating FromPersistence(int value)
	{
		return new(value);
	}
	#endregion

	#region Equality
	/// <summary>
	/// Computes hash code based on the rating value.
	/// </summary>
	protected override int GetHashCodeCore()
	{
		return Value.GetHashCode();
	}

	/// <summary>
	/// Compares two <see cref="ReviewRating"/> instances by their numeric value.
	/// </summary>
	/// <param name="other">Other rating instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(ReviewRating other)
	{
		return Value == other.Value;
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns a human-readable star representation (e.g. "5★").
	/// </summary>
	/// <returns>Star rating string.</returns>
	public override string ToString()
	{
		return $"{Value}★";
	}
	#endregion
}