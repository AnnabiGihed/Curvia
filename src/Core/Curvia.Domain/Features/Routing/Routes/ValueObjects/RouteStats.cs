using Pivot.Framework.Domain.Shared;
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
	public FunScore? FunScore { get; }
	public Duration? EstimatedDuration { get; }
	public ElevationGain? ElevationGain { get; }
	#endregion

	#region Constructors
	private RouteStats() { }

	private RouteStats(Distance distance, Duration? estimatedDuration, ElevationGain? elevationGain, FunScore? funScore)
	{
		Distance = distance;
		FunScore = funScore;
		ElevationGain = elevationGain;
		EstimatedDuration = estimatedDuration;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a new <see cref="RouteStats"/> instance.
	/// </summary>
	/// <param name="distance">Mandatory route or segment distance.</param>
	/// <param name="estimatedDuration">Optional estimated duration.</param>
	/// <param name="elevationGain">Optional elevation gain.</param>
	/// <param name="funScore">Optional computed fun score.</param>
	/// <returns>Successful result when valid; otherwise failure.</returns>
	public static Result<RouteStats> Create(Distance distance, Duration? estimatedDuration = null, ElevationGain? elevationGain = null, FunScore? funScore = null)
	{
		if (distance is null)
			return Result.Failure<RouteStats>(Error.NullValue);

		return Result.Success(
			new RouteStats(distance, estimatedDuration, elevationGain, funScore));
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