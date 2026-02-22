using Templates.Core.Domain.Shared;

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
	public string Value { get; }
	#endregion

	#region Constructors
	private ReviewComment() => Value = default!;
	private ReviewComment(string value) => Value = value;
	#endregion

	#region Factory
	public static Result<ReviewComment> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<ReviewComment>(new Error("ReviewComment.Empty", "Comment must not be empty."));

		if (value.Length > 2000)
			return Result.Failure<ReviewComment>(new Error("ReviewComment.TooLong", "Comment must not exceed 2,000 characters."));

		return Result.Success(new ReviewComment(value.Trim()));
	}

	/// <summary>For EF Core materialization only. Bypasses domain validation — DB values are assumed valid.</summary>
	public static ReviewComment FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	protected override bool EqualsCore(ReviewComment other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);
	#endregion

	#region Overrides
	public override string ToString() => Value;
	#endregion
}