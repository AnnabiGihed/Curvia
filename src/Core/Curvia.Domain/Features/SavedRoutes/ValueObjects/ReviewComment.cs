using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed value object for a free-text review comment.
///              Max 2,000 chars, trimmed.
///              Optional on RouteReview — entity holds ReviewComment? (nullable).
///              A reviewer may submit a rating-only review with no comment.
/// </summary>
public sealed class ReviewComment : CSharpFunctionalExtensions.ValueObject<ReviewComment>
{
	#region Properties
	/// <summary>
	/// Gets the textual review comment.
	/// </summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>
	/// Parameterless constructor required by ORMs.
	/// </summary>
	private ReviewComment()
	{
		Value = default!;
	}

	/// <summary>
	/// Creates a new <see cref="ReviewComment"/> instance.
	/// </summary>
	/// <param name="value">Validated comment value.</param>
	private ReviewComment(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="ReviewComment"/> after validating
	/// that the comment is non-empty and does not exceed 2,000 characters.
	/// </summary>
	/// <param name="value">Raw comment value.</param>
	/// <returns>A successful result containing <see cref="ReviewComment"/>, or a failure with domain error.</returns>
	public static Result<ReviewComment> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<ReviewComment>(new Error("ReviewComment.Empty", "Comment must not be empty."));

		if (value.Length > 2000)
			return Result.Failure<ReviewComment>(new Error("ReviewComment.TooLong", "Comment must not exceed 2,000 characters."));

		return Result.Success(new ReviewComment(value.Trim()));
	}

	/// <summary>
	/// For EF Core materialization only.
	/// Bypasses domain validation — DB values are assumed valid.
	/// </summary>
	/// <param name="value">Persisted comment value.</param>
	/// <returns><see cref="ReviewComment"/> instance.</returns>
	public static ReviewComment FromPersistence(string value)
	{
		return new(value);
	}
	#endregion

	#region Equality
	/// <summary>
	/// Computes hash code based on the comment value.
	/// </summary>
	protected override int GetHashCodeCore()
	{
		return StringComparer.Ordinal.GetHashCode(Value);
	}

	/// <summary>
	/// Compares two <see cref="ReviewComment"/> instances by their textual value.
	/// </summary>
	/// <param name="other">Other comment instance.</param>
	/// <returns>True if values are equal; otherwise false.</returns>
	protected override bool EqualsCore(ReviewComment other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}
	#endregion

	#region Overrides
	/// <summary>
	/// Returns the raw comment text.
	/// </summary>
	/// <returns>Comment string.</returns>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}