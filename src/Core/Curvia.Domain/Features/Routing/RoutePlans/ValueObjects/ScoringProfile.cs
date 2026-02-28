using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.Enums;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Defines the scoring configuration used by the fun-routing pipeline.
///              Carries the <see cref="FunFactor"/> global multiplier and the per-dimension
///              <see cref="ScoringWeights"/> (curves, elevation, scenery).
///              FunFactor controls how aggressively the engine trades extra distance for enjoyment —
///              higher values push Valhalla further off the direct corridor in search of fun roads.
///              Named preset factories provide ready-made profiles for one-tap ride personality
///              selection on the mobile app; <see cref="RoutePreset.Custom"/> lets the rider
///              tune every dimension individually.
/// </summary>
public sealed class ScoringProfile : CSharpFunctionalExtensions.ValueObject<ScoringProfile>
{
	#region Properties
	/// <summary>
	/// Global multiplier applied to the fun component in the routing cost function.
	/// Higher values produce more aggressive detours in favour of enjoyable roads.
	/// Range: [0.0, 10.0].
	/// </summary>
	public double FunFactor { get; }

	/// <summary>Per-component scoring weights (curves, elevation, scenery).</summary>
	public ScoringWeights Weights { get; }
	#endregion

	#region Constructors
	/// <summary>For EF Core materialisation only.</summary>
	private ScoringProfile() { Weights = default!; }

	/// <summary>Initialises a <see cref="ScoringProfile"/> with a pre-validated fun factor and weights.</summary>
	/// <param name="funFactor">Validated global fun multiplier.</param>
	/// <param name="weights">Validated per-dimension scoring weights.</param>
	private ScoringProfile(double funFactor, ScoringWeights weights)
	{
		FunFactor = funFactor;
		Weights = weights;
	}
	#endregion

	#region Factory — Custom
	/// <summary>
	/// Creates a custom <see cref="ScoringProfile"/> with explicit fun factor and weights.
	/// </summary>
	/// <param name="funFactor">Global fun multiplier. Range: 0.0 – 10.0.</param>
	/// <param name="weights">Per-component scoring weights. At least one must be greater than zero.</param>
	/// <returns>Success containing the <see cref="ScoringProfile"/>; otherwise failure.</returns>
	public static Result<ScoringProfile> Create(double funFactor, ScoringWeights weights)
	{
		if (weights is null)
			return Result.Failure<ScoringProfile>(RoutingErrors.NullValue(nameof(weights)));

		if (double.IsNaN(funFactor) || double.IsInfinity(funFactor))
			return Result.Failure<ScoringProfile>(new Error("Routing.ScoringProfile.NonFinite", "FunFactor must be a finite number."));

		if (funFactor is < 0.0 or > 10.0)
			return Result.Failure<ScoringProfile>(RoutingErrors.InvalidFunFactor(funFactor));

		return Result.Success(new ScoringProfile(funFactor, weights));
	}
	#endregion

	#region Factory — Named Presets
	/// <summary>
	/// Creates the <b>Twisty</b> preset: maximises twisty, flowing roads.
	/// Curves=0.80, Elevation=0.10, Scenery=0.10, FunFactor=9.
	/// Ideal for sport/naked riders who love technical corners.
	/// </summary>
	/// <returns>A pre-configured <see cref="ScoringProfile"/> for twisty riding.</returns>
	public static ScoringProfile Twisty()
	{
		var weights = ScoringWeights.Create(curves: 0.80, elevation: 0.10, scenery: 0.10).Value;
		return new ScoringProfile(funFactor: 9.0, weights);
	}

	/// <summary>
	/// Creates the <b>Panoramic</b> preset: prioritises mountain passes and scenic landscapes.
	/// Curves=0.20, Elevation=0.40, Scenery=0.40, FunFactor=7.
	/// Ideal for touring/adventure riders seeking dramatic views.
	/// </summary>
	/// <returns>A pre-configured <see cref="ScoringProfile"/> for panoramic riding.</returns>
	public static ScoringProfile Panoramic()
	{
		var weights = ScoringWeights.Create(curves: 0.20, elevation: 0.40, scenery: 0.40).Value;
		return new ScoringProfile(funFactor: 7.0, weights);
	}

	/// <summary>
	/// Creates the <b>Balanced</b> preset: a well-rounded mix of curves, elevation and scenery.
	/// Curves=0.50, Elevation=0.25, Scenery=0.25, FunFactor=8.
	/// Good default for riders who want a varied, enjoyable ride.
	/// </summary>
	/// <returns>A pre-configured <see cref="ScoringProfile"/> for balanced riding.</returns>
	public static ScoringProfile Balanced()
	{
		var weights = ScoringWeights.Create(curves: 0.50, elevation: 0.25, scenery: 0.25).Value;
		return new ScoringProfile(funFactor: 8.0, weights);
	}

	/// <summary>
	/// Creates the <b>Hilly</b> preset: maximises elevation gain and mountain relief.
	/// Curves=0.40, Elevation=1.00, Scenery=0.40, FunFactor=8.
	/// Ideal for riders who love mountain passes and sustained climbs.
	/// </summary>
	/// <returns>A pre-configured <see cref="ScoringProfile"/> for hilly riding.</returns>
	public static ScoringProfile Hilly()
	{
		var weights = ScoringWeights.Create(curves: 0.40, elevation: 1.00, scenery: 0.40).Value;
		return new ScoringProfile(funFactor: 8.0, weights);
	}

	/// <summary>
	/// Creates the <b>Scenic</b> preset: prioritises scenic surroundings — forests, lakes, ridges.
	/// Curves=0.30, Elevation=0.40, Scenery=1.00, FunFactor=7.
	/// Ideal for touring riders who want beautiful surroundings above all else.
	/// </summary>
	/// <returns>A pre-configured <see cref="ScoringProfile"/> for scenic riding.</returns>
	public static ScoringProfile Scenic()
	{
		var weights = ScoringWeights.Create(curves: 0.30, elevation: 0.40, scenery: 1.00).Value;
		return new ScoringProfile(funFactor: 7.0, weights);
	}

	/// <summary>
	/// Creates the <b>MaxFun</b> preset: maximum enjoyment across all three dimensions.
	/// Curves=1.00, Elevation=0.80, Scenery=0.60, FunFactor=10.
	/// Every scoring dial is turned up — the most adventurous routing possible.
	/// </summary>
	/// <returns>A pre-configured <see cref="ScoringProfile"/> for maximum fun riding.</returns>
	public static ScoringProfile MaxFun()
	{
		var weights = ScoringWeights.Create(curves: 1.00, elevation: 0.80, scenery: 0.60).Value;
		return new ScoringProfile(funFactor: 10.0, weights);
	}

	/// <summary>
	/// Resolves a <see cref="RoutePreset"/> to a <see cref="ScoringProfile"/>.
	/// For <see cref="RoutePreset.Custom"/>, supply <paramref name="customFunFactor"/> and <paramref name="customWeights"/>.
	/// </summary>
	/// <param name="preset">The desired preset.</param>
	/// <param name="customFunFactor">Required when <paramref name="preset"/> is <see cref="RoutePreset.Custom"/>. Range: 0.0 – 10.0.</param>
	/// <param name="customWeights">Required when <paramref name="preset"/> is <see cref="RoutePreset.Custom"/>.</param>
	/// <returns>Success containing the resolved <see cref="ScoringProfile"/>; otherwise failure.</returns>
	public static Result<ScoringProfile> FromPreset(RoutePreset preset, double? customFunFactor = null, ScoringWeights? customWeights = null)
	{
		return preset switch
		{
			RoutePreset.Twisty => Result.Success(Twisty()),
			RoutePreset.Panoramic => Result.Success(Panoramic()),
			RoutePreset.Balanced => Result.Success(Balanced()),
			RoutePreset.Hilly => Result.Success(Hilly()),
			RoutePreset.Scenic => Result.Success(Scenic()),
			RoutePreset.MaxFun => Result.Success(MaxFun()),
			RoutePreset.Custom => BuildCustom(customFunFactor, customWeights),
			_ => Result.Failure<ScoringProfile>(new Error("Routing.ScoringProfile.UnknownPreset", $"Unknown RoutePreset value '{(int)preset}'."))
		};
	}

	private static Result<ScoringProfile> BuildCustom(double? funFactor, ScoringWeights? weights)
	{
		if (funFactor is null)
			return Result.Failure<ScoringProfile>(new Error("Routing.ScoringProfile.CustomFunFactorRequired", "FunFactor is required when using the Custom preset."));

		if (weights is null)
			return Result.Failure<ScoringProfile>(new Error("Routing.ScoringProfile.CustomWeightsRequired", "ScoringWeights are required when using the Custom preset."));

		return Create(funFactor.Value, weights);
	}
	#endregion

	#region Equality
	/// <inheritdoc/>
	protected override bool EqualsCore(ScoringProfile other) => FunFactor.Equals(other.FunFactor) && Weights.Equals(other.Weights);

	/// <inheritdoc/>
	protected override int GetHashCodeCore() => HashCode.Combine(FunFactor, Weights);
	#endregion

	#region Overrides
	/// <inheritdoc/>
	public override string ToString() => $"FunFactor:{FunFactor:F1} | {Weights}";
	#endregion
}