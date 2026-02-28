using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Optional extended description for awareness entities (Hazard, RoadWork, Incident).
///              Null on the aggregate means "no description provided" — callers check for
///              null/empty BEFORE calling Create. Null must never enter a Result value.
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
	private AwarenessDescription()
	{
		Value = default!;
	}

	/// <summary>Initialises an <see cref="AwarenessDescription"/> with a validated value.</summary>
	/// <param name="value">Validated, non-empty description text.</param>
	private AwarenessDescription(string value)
	{
		Value = value;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="AwarenessDescription"/> from a non-null, non-empty string.
	/// Returns a failure result when the input exceeds <see cref="MaxLength"/>.
	/// </summary>
	/// <param name="value">Non-empty description text. Trimmed before storage.</param>
	/// <returns>Successful result with the value object; failure if too long.</returns>
	public static Result<AwarenessDescription> Create(string value)
	{
		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<AwarenessDescription>(new Error("AwarenessDescription.TooLong", $"Description must not exceed {MaxLength} characters."));

		return Result.Success(new AwarenessDescription(trimmed));
	}

	/// <summary>
	/// Handles the optional description pattern — returns null when input is null or whitespace,
	/// or a failure when the input exceeds <see cref="MaxLength"/>.
	/// Uses a tuple return instead of Result so that null never travels inside a Result value,
	/// which the Result framework forbids.
	/// </summary>
	/// <param name="value">Raw description input. Null or whitespace yields a null Value with no Error.</param>
	/// <returns>A tuple of (Value, Error) — both null on empty input; Error set on validation failure; Value set on success.</returns>
	public static (AwarenessDescription? Value, Error? Error) CreateOptional(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return (null, null);

		var result = Create(value);

		if (result.IsFailure)
			return (null, result.Error);

		return (result.Value, null);
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored description value.</param>
	/// <returns>An <see cref="AwarenessDescription"/> without validation.</returns>
	public static AwarenessDescription FromPersistence(string value)
	{
		return new AwarenessDescription(value);
	}
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(AwarenessDescription other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}

	/// <inheritdoc/>
	protected override int GetHashCodeCore()
	{
		return Value.GetHashCode(StringComparison.Ordinal);
	}
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="AwarenessDescription"/> where a nullable string is expected.</summary>
	/// <param name="vo">The value object to convert, or null.</param>
	public static implicit operator string?(AwarenessDescription? vo)
	{
		return vo?.Value;
	}
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString()
	{
		return Value;
	}
	#endregion
}