using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Short human-readable title for a <see cref="Aggregates.RoadWork"/> awareness entity.
///              Must not be null or empty.  Max length: 200 characters.
/// </summary>
public sealed class AwarenessTitle : CSharpFunctionalExtensions.ValueObject<AwarenessTitle>
{
	#region Constants
	/// <summary>Maximum allowed length for an awareness title.</summary>
	public const int MaxLength = 200;
	#endregion

	#region Properties
	/// <summary>The trimmed title text.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private AwarenessTitle() { Value = default!; }

	/// <summary>Initialises an <see cref="AwarenessTitle"/> with a validated value.</summary>
	/// <param name="value">Validated title text.</param>
	private AwarenessTitle(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="AwarenessTitle"/>.
	/// </summary>
	/// <param name="value">Raw title input.  Trimmed before storage.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<AwarenessTitle> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<AwarenessTitle>(new Error("AwarenessTitle.Required", "Title is required."));

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<AwarenessTitle>(new Error("AwarenessTitle.TooLong", $"Title must not exceed {MaxLength} characters."));

		return Result.Success(new AwarenessTitle(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored title value.</param>
	/// <returns>An <see cref="AwarenessTitle"/> without validation.</returns>
	public static AwarenessTitle FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(AwarenessTitle other) =>
		string.Equals(Value, other.Value, StringComparison.Ordinal);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode(StringComparison.Ordinal);
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="AwarenessTitle"/> where a <see cref="string"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator string(AwarenessTitle vo) => vo.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}