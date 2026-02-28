using Pivot.Framework.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Provider-assigned external identifier for an awareness entity
///              (e.g. "osm:node/12345678", "gipod:98765").
///              Together with <see cref="SourceName"/> and <see cref="AwarenessCountryCode"/>
///              this forms the natural composite key used by the Worker sync pipeline
///              for idempotent upserts.
///              Max length: 256 characters.
/// </summary>
public sealed class ExternalId : CSharpFunctionalExtensions.ValueObject<ExternalId>
{
	#region Constants
	/// <summary>Maximum allowed length of the external identifier.</summary>
	public const int MaxLength = 256;
	#endregion

	#region Properties
	/// <summary>The raw external identifier string as provided by the data source.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private ExternalId() { Value = default!; }

	/// <summary>Initialises an <see cref="ExternalId"/> with a validated value.</summary>
	/// <param name="value">Validated external identifier.</param>
	private ExternalId(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="ExternalId"/>.
	/// </summary>
	/// <param name="value">Raw external identifier.  Must not be null, empty, or exceed 256 characters.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<ExternalId> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<ExternalId>(new Error("ExternalId.Required", "ExternalId is required."));

		if (value.Length > MaxLength)
			return Result.Failure<ExternalId>(new Error("ExternalId.TooLong", $"ExternalId must not exceed {MaxLength} characters."));

		return Result.Success(new ExternalId(value.Trim()));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored external identifier.</param>
	/// <returns>An <see cref="ExternalId"/> without validation.</returns>
	public static ExternalId FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(ExternalId other) =>
		string.Equals(Value, other.Value, StringComparison.Ordinal);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.GetHashCode(StringComparison.Ordinal);
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}