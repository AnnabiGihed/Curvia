using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed optional user-supplied description for a <c>SavedRoute</c>.
///              Replaces the raw <c>string? Description</c> field on the <c>SavedRoute</c>
///              aggregate to enforce the maximum length constraint at the domain boundary
///              even when the value is provided.
///              Persisted as NVARCHAR(2000) — generous enough for a ride journal entry.
/// </summary>
public sealed class SavedRouteDescription : CSharpFunctionalExtensions.ValueObject<SavedRouteDescription>
{
	#region Constants
	/// <summary>Maximum allowed character length for a saved route description.</summary>
	public const int MaxLength = 2_000;
	#endregion

	#region Properties
	/// <summary>The description text. Never null, empty, or whitespace-only.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private SavedRouteDescription() { Value = default!; }

	/// <summary>Initialises a <see cref="SavedRouteDescription"/> with a pre-validated value.</summary>
	/// <param name="value">Validated description string.</param>
	private SavedRouteDescription(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="SavedRouteDescription"/>.
	/// </summary>
	/// <param name="value">Raw description. Will be trimmed. Must not be null or whitespace, and must not exceed <see cref="MaxLength"/> characters.</param>
	/// <returns>Success containing the <see cref="SavedRouteDescription"/>; otherwise failure.</returns>
	public static Result<SavedRouteDescription> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<SavedRouteDescription>(new Error("SavedRouteDescription.Required", "Description must not be empty. Omit the field entirely instead of passing an empty string."));

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<SavedRouteDescription>(new Error("SavedRouteDescription.TooLong", $"Description exceeds the maximum length of {MaxLength} characters."));

		return Result.Success(new SavedRouteDescription(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored description string.</param>
	/// <returns>A <see cref="SavedRouteDescription"/> without validation.</returns>
	public static SavedRouteDescription FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(SavedRouteDescription other) =>
		string.Equals(Value, other.Value, StringComparison.Ordinal);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value.Length > 100 ? Value[..100] + "…" : Value;
	#endregion
}