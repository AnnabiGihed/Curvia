using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Captures all routing constraints applied during route candidate generation.
///              Hard constraints (MaxDetourRatio, MaxDurationSeconds) immediately disqualify candidates.
///              Soft constraints (MaxDistance, surface/urban preferences) apply scoring penalties
///              or Valhalla costing adjustments.
/// </summary>
public sealed class RoutingConstraints : CSharpFunctionalExtensions.ValueObject<RoutingConstraints>
{
	#region Properties

	/// <summary>
	/// Maximum allowed route distance as a ratio over the baseline distance.
	/// Example: 2.0 allows a route up to twice the straight-line distance.
	/// Must be >= 1.0.
	/// </summary>
	public double MaxDetourRatio { get; }

	/// <summary>Optional hard cap on total route distance in meters.</summary>
	public Distance? MaxDistance { get; }

	/// <summary>
	/// Optional hard cap on total route duration in seconds.
	/// Candidates exceeding this limit are immediately disqualified.
	/// Leave null for unlimited ride time.
	/// </summary>
	public long? MaxDurationSeconds { get; }

	/// <summary>When true, motorways and dual carriageways are strongly avoided.</summary>
	public bool AvoidHighways { get; }

	/// <summary>When true, toll roads are strongly avoided.</summary>
	public bool AvoidTolls { get; }

	/// <summary>
	/// When true, unpaved roads, gravel tracks and off-road trails are avoided.
	/// Maps to Valhalla's <c>use_trails = 0.0</c>.
	/// </summary>
	public bool AvoidUnpaved { get; }

	/// <summary>
	/// Controls how aggressively urban streets and town centres are avoided.
	/// Default: <see cref="UrbanTolerance.Allowed"/>.
	/// Maps to Valhalla's <c>use_living_streets</c>.
	/// </summary>
	public UrbanTolerance UrbanTolerance { get; }

	#endregion

	#region Constructors
	/// <summary>For EF Core.</summary>
	private RoutingConstraints() { }

	private RoutingConstraints(double maxDetourRatio, Distance? maxDistance, long? maxDurationSeconds, bool avoidHighways, bool avoidTolls, bool avoidUnpaved, UrbanTolerance urbanTolerance)
	{
		AvoidTolls = avoidTolls;
		MaxDistance = maxDistance;
		AvoidUnpaved = avoidUnpaved;
		AvoidHighways = avoidHighways;
		MaxDetourRatio = maxDetourRatio;
		UrbanTolerance = urbanTolerance;
		MaxDurationSeconds = maxDurationSeconds;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="RoutingConstraints"/> instance.
	/// </summary>
	/// <param name="maxDetourRatio">Minimum 1.0 (no detour). Typical range: 1.5 – 3.0.</param>
	/// <param name="avoidHighways">Strongly avoids motorways and dual carriageways.</param>
	/// <param name="avoidTolls">Strongly avoids toll roads.</param>
	/// <param name="avoidUnpaved">Avoids unpaved, gravel and track roads.</param>
	/// <param name="urbanTolerance">How aggressively to avoid towns. Default: Allowed.</param>
	/// <param name="maxDistance">Optional absolute distance cap (meters).</param>
	/// <param name="maxDurationSeconds">Optional absolute duration cap (seconds).</param>
	public static Result<RoutingConstraints> Create(double maxDetourRatio, bool avoidHighways = false, bool avoidTolls = false, bool avoidUnpaved = false, UrbanTolerance urbanTolerance = UrbanTolerance.Allowed, Distance? maxDistance = null, long? maxDurationSeconds = null)
	{
		if (double.IsNaN(maxDetourRatio) || double.IsInfinity(maxDetourRatio))
			return Result.Failure<RoutingConstraints>(new Error("Routing.Constraints.NonFinite", "MaxDetourRatio must be a finite number."));

		if (maxDetourRatio < 1.0)
			return Result.Failure<RoutingConstraints>(RoutingErrors.InvalidDetourRatio(maxDetourRatio));

		if (maxDurationSeconds.HasValue && maxDurationSeconds.Value <= 0)
			return Result.Failure<RoutingConstraints>(new Error("Routing.Constraints.InvalidDuration", "MaxDurationSeconds must be greater than 0."));

		if (!Enum.IsDefined(urbanTolerance))
			return Result.Failure<RoutingConstraints>(new Error("Routing.Constraints.InvalidUrbanTolerance", $"Unknown UrbanTolerance value '{(int)urbanTolerance}'."));

		return Result.Success(new RoutingConstraints(maxDetourRatio, maxDistance, maxDurationSeconds, avoidHighways, avoidTolls, avoidUnpaved, urbanTolerance));
	}
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(RoutingConstraints other)
	{
		return MaxDetourRatio.Equals(other.MaxDetourRatio)
			   && Equals(MaxDistance, other.MaxDistance)
			   && MaxDurationSeconds == other.MaxDurationSeconds
			   && AvoidHighways == other.AvoidHighways
			   && AvoidTolls == other.AvoidTolls
			   && AvoidUnpaved == other.AvoidUnpaved
			   && UrbanTolerance == other.UrbanTolerance;
	}

	/// <inheritdoc/>
	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(MaxDetourRatio, MaxDistance, MaxDurationSeconds, HashCode.Combine(AvoidHighways, AvoidTolls, AvoidUnpaved, UrbanTolerance));
	}
	#endregion
}