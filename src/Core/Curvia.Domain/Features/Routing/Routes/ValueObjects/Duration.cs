using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a duration in seconds.
///              Intended for ETA / segment durations.
/// </summary>
public sealed class Duration : CSharpFunctionalExtensions.ValueObject<Duration>
{
	#region Properties

	public long Seconds { get; }

	public static Duration Zero => new(0);

	#endregion

	#region Constructor

	private Duration(long seconds)
	{
		Seconds = seconds;
	}

	#endregion

	#region Factory

	public static Result<Duration> Create(long seconds)
	{
		if (seconds < 0)
			return Result.Failure<Duration>(RoutingErrors.DurationNegative(seconds));

		return Result.Success(new Duration(seconds));
	}

	#endregion

	#region Methods

	public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Seconds);

	#endregion

	#region Equality

	protected override bool EqualsCore(Duration other)
		=> Seconds == other.Seconds;

	protected override int GetHashCodeCore()
		=> Seconds.GetHashCode();

	#endregion

	#region Overrides

	public override string ToString() => $"{Seconds}s";

	#endregion
}
