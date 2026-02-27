using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Awareness.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : ISO 3166-1 alpha-2 country code for awareness entities (e.g. "BE", "FR", "DE").
///              Stored as two uppercase ASCII characters.  Validated on construction;
///              persisted as a two-character string column.
/// </summary>
public sealed class AwarenessCountryCode : CSharpFunctionalExtensions.ValueObject<AwarenessCountryCode>
{
	#region Properties
	/// <summary>Uppercase two-letter ISO 3166-1 alpha-2 country code.</summary>
	public string Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private AwarenessCountryCode() { Value = default!; }

	/// <summary>Initialises a <see cref="AwarenessCountryCode"/> with a validated value.</summary>
	/// <param name="value">Validated, uppercase country code.</param>
	private AwarenessCountryCode(string value) => Value = value;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="AwarenessCountryCode"/>.
	/// </summary>
	/// <param name="value">Raw country code input.  Will be trimmed and uppercased.</param>
	/// <returns>Successful result when the code is exactly two ASCII letters; otherwise failure.</returns>
	public static Result<AwarenessCountryCode> Create(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<AwarenessCountryCode>(new Error("AwarenessCountryCode.Required", "Country code is required."));

		var normalised = value.Trim().ToUpperInvariant();

		if (normalised.Length != 2 || !normalised.All(char.IsLetter))
			return Result.Failure<AwarenessCountryCode>(new Error("AwarenessCountryCode.Invalid", $"'{value}' is not a valid ISO 3166-1 alpha-2 country code."));

		return Result.Success(new AwarenessCountryCode(normalised));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="value">Stored country code value.</param>
	/// <returns>A <see cref="AwarenessCountryCode"/> without validation.</returns>
	public static AwarenessCountryCode FromPersistence(string value) => new(value);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(AwarenessCountryCode other) =>
		string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value.ToUpperInvariant().GetHashCode();
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="AwarenessCountryCode"/> where a <see cref="string"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator string(AwarenessCountryCode vo) => vo.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value;
	#endregion
}