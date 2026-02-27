using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.SavedRoutes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Strongly-typed user-supplied title for a <c>SavedRoute</c>.
///              Replaces the raw <c>string Title</c> field on the <c>SavedRoute</c> aggregate
///              to enforce non-empty and length constraints at the domain boundary.
///              Persisted as NVARCHAR(150) — long enough for descriptive titles while preventing
///              runaway input.
/// </summary>
public sealed class SavedRouteTitle : CSharpFunctionalExtensions.ValueObject<SavedRouteTitle>
{
	#region Constants
	/// <summary>Maximum allowed character length for a saved route title.</summary>
	public const int MaxLength = 150;
	#endregion

	#region Properties
	/// <summary>The user-supplied title string. Never null, empty, or whitespace-only.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private SavedRouteTitle() { Value = default!; }

	/// <summary>Initialises a <see cref="SavedRouteTitle"/> with a pre-validated value.</summary>
	/// <param name="value">Validated title string.</param>
	private SavedRouteTitle(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="SavedRouteTitle"/>.
	/// </summary>
	/// <param name="value">Raw title. Will be trimmed. Must not be null or whitespace, and must not exceed <see cref="MaxLength"/> characters.</param>
	/// <returns>Success containing the <see cref="SavedRouteTitle"/>; otherwise failure.</returns>
	public static Result<SavedRouteTitle> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<SavedRouteTitle>(new Error("SavedRouteTitle.Required", "Saved route title is required."));

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<SavedRouteTitle>(new Error("SavedRouteTitle.TooLong", $"Saved route title exceeds the maximum length of {MaxLength} characters."));

		return Result.Success(new SavedRouteTitle(trimmed));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored title string.</param>
	/// <returns>A <see cref="SavedRouteTitle"/> without validation.</returns>
	public static SavedRouteTitle FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(SavedRouteTitle other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToLowerInvariant().GetHashCode();
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}