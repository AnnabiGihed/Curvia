using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Statistics computed for a route or segment (distance, duration, elevation gain, fun score).
/// </summary>
public sealed class RouteStats : CSharpFunctionalExtensions.ValueObject<RouteStats>
{
	#region Properties

	public Distance Distance { get; }
	public Duration? EstimatedDuration { get; }
	public ElevationGain? ElevationGain { get; }
	public FunScore? FunScore { get; }

	#endregion

	#region Constructor

	private RouteStats(Distance distance, Duration? estimatedDuration, ElevationGain? elevationGain, FunScore? funScore)
	{
		Distance = distance;
		EstimatedDuration = estimatedDuration;
		ElevationGain = elevationGain;
		FunScore = funScore;
	}

	#endregion

	#region Factory

	public static Result<RouteStats> Create(
		Distance distance,
		Duration? estimatedDuration = null,
		ElevationGain? elevationGain = null,
		FunScore? funScore = null)
	{
		if (distance is null)
			return Result.Failure<RouteStats>(Error.NullValue);

		return Result.Success(new RouteStats(distance, estimatedDuration, elevationGain, funScore));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(RouteStats other)
	{
		return Distance.Equals(other.Distance)
			   && Equals(EstimatedDuration, other.EstimatedDuration)
			   && Equals(ElevationGain, other.ElevationGain)
			   && Equals(FunScore, other.FunScore);
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(Distance, EstimatedDuration, ElevationGain, FunScore);
	}

	#endregion
}