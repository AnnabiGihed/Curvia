using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

namespace Curvia.Application.Features.Routes.Contracts.Services.Scoring;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for scoring Valhalla route candidates.
///              Scoring uses real geometric analysis (curvature from polyline, elevation from /height)
///              and applies constraint guards (detour ratio, duration cap, distance cap).
///              Returns a scalar score where higher = more fun and constraint-compliant.
/// </summary>
public interface IRouteScoringService
{
	/// <summary>
	/// Scores a single Valhalla route candidate against the routing plan's constraints and scoring profile.
	/// </summary>
	/// <param name="candidate">Raw Valhalla response for this candidate.</param>
	/// <param name="plan">The routing plan containing constraints and scoring weights.</param>
	/// <param name="variantName">
	/// The variant name that generated this candidate (e.g. "balanced", "scenic", "explorer").
	/// Used for minor tiebreaking and telemetry; the primary score is geometry-derived.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// Scalar score. Higher = more fun and constraint-compliant.
	/// Returns <see cref="double.NegativeInfinity"/> for null/invalid candidates.
	/// Returns -1,000,000 for candidates that violate hard constraints.
	/// </returns>
	Task<double> ScoreAsync(RouteCandidate candidate, RoutePlan plan, CancellationToken cancellationToken = default);
}