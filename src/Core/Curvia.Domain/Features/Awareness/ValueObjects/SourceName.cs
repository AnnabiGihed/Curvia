using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Short, machine-readable name of the data provider that supplied an
///              awareness entity (e.g. "osm", "openspeedcam", "gipod").
///              Used as part of the composite natural key (ExternalId + SourceName + CountryCode)
///              for idempotent upserts and as the discriminator in DeleteBySourceAsync.
///              Max length: 64 characters, lowercase enforced.
/// </summary>
public sealed class SourceName : CSharpFunctionalExtensions.ValueObject<SourceName>
{
	#region Constants
	/// <summary>Maximum allowed length for a source name.</summary>
	public const int MaxLength = 64;
	#endregion

	#region Properties
	/// <summary>Lowercase provider name (e.g. "osm", "openspeedcam").</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private SourceName() { Value = default!; }

	/// <summary>Initialises a <see cref="SourceName"/> with a validated value.</summary>
	/// <param name="value">Validated, lowercased source name.</param>
	private SourceName(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="SourceName"/>.
	/// </summary>
	/// <param name="value">Raw source name input.  Will be trimmed and lowercased.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<SourceName> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<SourceName>(new Error("SourceName.Required", "Source name is required."));

		var normalised = value.Trim().ToLowerInvariant();

		if (normalised.Length > MaxLength)
			return Result.Failure<SourceName>(new Error("SourceName.TooLong", $"Source name must not exceed {MaxLength} characters."));

		return Result.Success(new SourceName(normalised));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored source name value.</param>
	/// <returns>A <see cref="SourceName"/> without validation.</returns>
	public static SourceName FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(SourceName other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToLowerInvariant().GetHashCode();
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="SourceName"/> where a <see cref="string"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator string(SourceName vo) => vo.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}