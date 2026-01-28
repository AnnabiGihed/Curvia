using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a distance in meters.
///              Provides safe creation and basic arithmetic helpers.
/// </summary>
public sealed class Distance : CSharpFunctionalExtensions.ValueObject<Distance>
{
	#region Properties

	public double Meters { get; }

	public static Distance Zero => new(0);

	#endregion

	#region Constructor
	private Distance()
	{
	}
	private Distance(double meters)
	{
		Meters = meters;
	}

	#endregion

	#region Factory

	public static Result<Distance> Create(double meters)
	{
		if (double.IsNaN(meters) || double.IsInfinity(meters))
			return Result.Failure<Distance>(RoutingErrors.NonFiniteNumber(nameof(meters)));

		if (meters < 0)
			return Result.Failure<Distance>(RoutingErrors.DistanceNegative(meters));

		return Result.Success(new Distance(meters));
	}

	#endregion

	#region Methods

	public Distance Add(Distance other) => new(Meters + other.Meters);

	#endregion

	#region Equality

	protected override bool EqualsCore(Distance other)
		=> Meters.Equals(other.Meters);

	protected override int GetHashCodeCore()
		=> Meters.GetHashCode();

	#endregion

	#region Overrides

	public override string ToString() => $"{Meters:0.##}m";

	#endregion
}
