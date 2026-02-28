using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared.ValueObjects;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Captures all routing constraints applied during route candidate generation.
///              Hard constraints (<see cref="MaxDetourRatio"/>, <see cref="MaxDurationSeconds"/>)
///              immediately disqualify candidates that violate them (score = -1,000,000).
///              Soft constraints (<see cref="MaxDistance"/>, surface and urban preferences)
///              apply scoring penalties or Valhalla costing adjustments.
///
///              Primitive-obsession fix applied:
///                - <see cref="MaxDetourRatio"/> is now a typed value object enforcing [1.0, 4.0].
///
///              Additions vs previous version:
///                - <see cref="AvoidFerries"/>: maps to Valhalla's <c>use_ferry = 0.0</c>.
///                - <see cref="AvoidMotorwayLinks"/>: documented intent; combined with highway avoidance.
/// </summary>
public sealed class RoutingConstraints : CSharpFunctionalExtensions.ValueObject<RoutingConstraints>
{
	#region Properties
	/// <summary>
	/// Maximum allowed route distance as a ratio over the baseline distance.
	/// Example: 2.0 allows a route up to twice the straight-line distance.
	/// Range: 1.0 – 4.0.
	/// </summary>
	public MaxDetourRatio MaxDetourRatio { get; }

	/// <summary>
	/// Optional hard cap on total route distance in metres.
	/// Candidates exceeding this limit receive a heavy scoring penalty proportional to the overage.
	/// Leave null for no distance cap.
	/// </summary>
	public Distance? MaxDistance { get; }

	/// <summary>
	/// Optional hard cap on total route duration in seconds.
	/// Candidates exceeding this limit are immediately disqualified (score = -1,000,000).
	/// Leave null for unlimited ride time.
	/// </summary>
	public long? MaxDurationSeconds { get; }

	/// <summary>When true, motorways and dual carriageways are strongly avoided.</summary>
	public bool AvoidHighways { get; }

	/// <summary>When true, toll roads are strongly avoided.</summary>
	public bool AvoidTolls { get; }

	/// <summary>When true, ferry crossings are avoided. Maps to Valhalla's <c>use_ferry = 0.0</c>.</summary>
	public bool AvoidFerries { get; }

	/// <summary>When true, unpaved roads, gravel tracks and off-road trails are avoided. Maps to Valhalla's <c>use_trails = 0.0</c>.</summary>
	public bool AvoidUnpaved { get; }

	/// <summary>
	/// When true, motorway on-ramps and link roads are avoided.
	/// Implemented by reducing the highway preference further in Valhalla costing.
	/// </summary>
	public bool AvoidMotorwayLinks { get; }

	/// <summary>
	/// Controls how aggressively urban streets and town centres are avoided.
	/// Maps to Valhalla's <c>use_living_streets</c> costing parameter.
	/// Default: <see cref="UrbanTolerance.Allowed"/>.
	/// </summary>
	public UrbanTolerance UrbanTolerance { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private RoutingConstraints() { MaxDetourRatio = default!; }

	/// <summary>Initialises a <see cref="RoutingConstraints"/> with all validated fields.</summary>
	private RoutingConstraints(MaxDetourRatio maxDetourRatio, Distance? maxDistance, long? maxDurationSeconds, bool avoidHighways, bool avoidTolls, bool avoidFerries, bool avoidUnpaved, bool avoidMotorwayLinks, UrbanTolerance urbanTolerance)
	{
		MaxDetourRatio = maxDetourRatio;
		MaxDistance = maxDistance;
		MaxDurationSeconds = maxDurationSeconds;
		AvoidHighways = avoidHighways;
		AvoidTolls = avoidTolls;
		AvoidFerries = avoidFerries;
		AvoidUnpaved = avoidUnpaved;
		AvoidMotorwayLinks = avoidMotorwayLinks;
		UrbanTolerance = urbanTolerance;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Creates a validated <see cref="RoutingConstraints"/> instance.
	/// </summary>
	/// <param name="maxDetourRatio">Maximum detour ratio. Range: 1.0 – 4.0.</param>
	/// <param name="avoidHighways">Strongly avoids motorways and dual carriageways.</param>
	/// <param name="avoidTolls">Strongly avoids toll roads.</param>
	/// <param name="avoidFerries">Avoids ferry crossings.</param>
	/// <param name="avoidUnpaved">Avoids unpaved, gravel and track roads.</param>
	/// <param name="avoidMotorwayLinks">Avoids motorway on-ramps and link roads.</param>
	/// <param name="urbanTolerance">How aggressively to avoid towns. Default: <see cref="UrbanTolerance.Allowed"/>.</param>
	/// <param name="maxDistance">Optional absolute distance cap in metres.</param>
	/// <param name="maxDurationSeconds">Optional absolute duration cap in seconds.</param>
	/// <returns>Success containing the <see cref="RoutingConstraints"/>; otherwise failure.</returns>
	public static Result<RoutingConstraints> Create(double maxDetourRatio, bool avoidHighways = false, bool avoidTolls = false, bool avoidFerries = false, bool avoidUnpaved = false, bool avoidMotorwayLinks = false, UrbanTolerance urbanTolerance = UrbanTolerance.Allowed, Distance? maxDistance = null, long? maxDurationSeconds = null)
	{
		var ratioResult = MaxDetourRatio.Create(maxDetourRatio);
		if (ratioResult.IsFailure)
			return Result.Failure<RoutingConstraints>(ratioResult.Error);

		if (maxDurationSeconds.HasValue && maxDurationSeconds.Value <= 0)
			return Result.Failure<RoutingConstraints>(new Error("Routing.Constraints.InvalidDuration", "MaxDurationSeconds must be greater than 0."));

		if (!Enum.IsDefined(urbanTolerance))
			return Result.Failure<RoutingConstraints>(new Error("Routing.Constraints.InvalidUrbanTolerance", $"Unknown UrbanTolerance value '{(int)urbanTolerance}'."));

		return Result.Success(new RoutingConstraints(ratioResult.Value, maxDistance, maxDurationSeconds, avoidHighways, avoidTolls, avoidFerries, avoidUnpaved, avoidMotorwayLinks, urbanTolerance));
	}

	/// <summary>
	/// Bypasses validation — used when materialising from a trusted persistence source.
	/// </summary>
	/// <returns>A <see cref="RoutingConstraints"/> without validation.</returns>
	public static RoutingConstraints FromPersistence(double maxDetourRatio, Distance? maxDistance, long? maxDurationSeconds, bool avoidHighways, bool avoidTolls, bool avoidFerries, bool avoidUnpaved, bool avoidMotorwayLinks, UrbanTolerance urbanTolerance) =>
		new(MaxDetourRatio.FromPersistence(maxDetourRatio), maxDistance, maxDurationSeconds, avoidHighways, avoidTolls, avoidFerries, avoidUnpaved, avoidMotorwayLinks, urbanTolerance);

	/// <summary>Sensible defaults: detour ×1.5, no avoidances, urban roads allowed.</summary>
	public static RoutingConstraints Default =>
		new(MaxDetourRatio.Default, null, null, false, false, false, false, false, UrbanTolerance.Allowed);
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(RoutingConstraints other) =>
		MaxDetourRatio.Equals(other.MaxDetourRatio) &&
		Equals(MaxDistance, other.MaxDistance) &&
		MaxDurationSeconds == other.MaxDurationSeconds &&
		AvoidHighways == other.AvoidHighways &&
		AvoidTolls == other.AvoidTolls &&
		AvoidFerries == other.AvoidFerries &&
		AvoidUnpaved == other.AvoidUnpaved &&
		AvoidMotorwayLinks == other.AvoidMotorwayLinks &&
		UrbanTolerance == other.UrbanTolerance;

	/// <inheritdoc/>
	protected override int GetHashCodeCore() =>
		HashCode.Combine(MaxDetourRatio, MaxDistance, MaxDurationSeconds, HashCode.Combine(AvoidHighways, AvoidTolls, AvoidFerries, AvoidUnpaved, AvoidMotorwayLinks, UrbanTolerance));
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() =>
		$"MaxDetour:{MaxDetourRatio} | Highways:{AvoidHighways} | Tolls:{AvoidTolls} | Ferries:{AvoidFerries} | Unpaved:{AvoidUnpaved} | Links:{AvoidMotorwayLinks} | Urban:{UrbanTolerance}";
	#endregion
}