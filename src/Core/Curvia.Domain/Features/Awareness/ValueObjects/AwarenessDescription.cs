using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Optional extended description for awareness entities (Hazard, RoadWork, Incident).
///              Null on the aggregate represents "no description provided".
///              Max length: 500 characters.
/// </summary>
public sealed class AwarenessDescription : CSharpFunctionalExtensions.ValueObject<AwarenessDescription>
{
	#region Constants
	/// <summary>Maximum allowed length for an awareness description.</summary>
	public const int MaxLength = 500;
	#endregion

	#region Properties
	/// <summary>The trimmed description text.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private AwarenessDescription() { Value = default!; }

	/// <summary>Initialises an <see cref="AwarenessDescription"/> with a validated value.</summary>
	/// <param name="value">Validated description text.</param>
	private AwarenessDescription(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="AwarenessDescription"/>.
	/// Returns null (not a failure) when the input is null or empty — callers should
	/// store the result as <c>AwarenessDescription?</c>.
	/// </summary>
	/// <param name="value">Raw description input.  Trimmed before storage.  Null/empty returns null.</param>
	/// <returns>Successful result with a non-null VO; <c>Result.Success(null)</c> when input is empty; failure if too long.</returns>
	public static Result<AwarenessDescription?> CreateOptional(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Success<AwarenessDescription?>(null);

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<AwarenessDescription?>(new Error("AwarenessDescription.TooLong", $"Description must not exceed {MaxLength} characters."));

		return Result.Success<AwarenessDescription?>(new AwarenessDescription(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored description value.</param>
	/// <returns>An <see cref="AwarenessDescription"/> without validation.</returns>
	public static AwarenessDescription FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(AwarenessDescription other) =>
		string.Equals(Value, other.Value, StringComparison.Ordinal);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode(StringComparison.Ordinal);
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="AwarenessDescription"/> where a nullable <see cref="string"/> is expected.</summary>
	/// <param name="vo">The value object to convert, or null.</param>
	public static implicit operator string?(AwarenessDescription? vo) => vo?.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}