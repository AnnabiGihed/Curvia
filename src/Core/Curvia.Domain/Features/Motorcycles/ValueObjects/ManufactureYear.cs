using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Motorcycles.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Model year for a <see cref="Aggregates.Motorcycle"/>.
///              Valid range: 1900 to current year + 2 (to allow pre-orders).
///              Prevents nonsensical year values from entering the domain.
/// </summary>
public sealed class ManufactureYear : CSharpFunctionalExtensions.ValueObject<ManufactureYear>
{
	#region Constants
	/// <summary>Earliest permitted manufacture year.</summary>
	public const int MinYear = 1900;
	#endregion

	#region Properties
	/// <summary>Four-digit model year.</summary>
	public int Value { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialization only.</summary>
	private ManufactureYear() { }

	/// <summary>Initialises a <see cref="ManufactureYear"/> with a validated value.</summary>
	/// <param name="year">Validated year.</param>
	private ManufactureYear(int year) => Value = year;
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="ManufactureYear"/>.
	/// </summary>
	/// <param name="year">Four-digit model year.  Must be between 1900 and current year + 2.</param>
	/// <returns>Successful result; otherwise failure.</returns>
	public static Result<ManufactureYear> Create(int year)
	{
		var maxYear = DateTime.UtcNow.Year + 2;

		if (year < MinYear || year > maxYear)
			return Result.Failure<ManufactureYear>(new Error("ManufactureYear.OutOfRange", $"Year '{year}' must be between {MinYear} and {maxYear}."));

		return Result.Success(new ManufactureYear(year));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <param name="year">Stored year value.</param>
	/// <returns>A <see cref="ManufactureYear"/> without validation.</returns>
	public static ManufactureYear FromPersistence(int year) => new(year);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(ManufactureYear other) => Value == other.Value;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => Value;
	#endregion

	#region Implicit conversion
	/// <summary>Allows transparent use of <see cref="ManufactureYear"/> where an <see cref="int"/> is expected.</summary>
	/// <param name="vo">The value object to convert.</param>
	public static implicit operator int(ManufactureYear vo) => vo.Value;
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => Value.ToString();
	#endregion
}