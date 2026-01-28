using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents elevation gain in meters (non-negative).
///              Used for route/segment statistics.
/// </summary>
public sealed class ElevationGain : CSharpFunctionalExtensions.ValueObject<ElevationGain>
{
	#region Properties

	public double Meters { get; }

	public static ElevationGain Zero => new(0);

	#endregion

	#region Constructor
	private ElevationGain()
	{
	}
	private ElevationGain(double meters)
	{
		Meters = meters;
	}

	#endregion

	#region Factory

	public static Result<ElevationGain> Create(double meters)
	{
		if (double.IsNaN(meters) || double.IsInfinity(meters))
			return Result.Failure<ElevationGain>(RoutingErrors.NonFiniteNumber(nameof(meters)));

		if (meters < 0)
			return Result.Failure<ElevationGain>(RoutingErrors.ElevationGainNegative(meters));

		return Result.Success(new ElevationGain(meters));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(ElevationGain other)
		=> Meters.Equals(other.Meters);

	protected override int GetHashCodeCore()
		=> Meters.GetHashCode();

	#endregion

	#region Overrides

	public override string ToString() => $"{Meters:0.##}m";

	#endregion
}
