using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for generating Valhalla route candidates.
/// </summary>
public interface IRouteCandidateGeneratorService
{
	/// <summary>
	/// Generates route candidates for the outbound journey.
	/// For point-to-point plans: Start → End variants.
	/// For loop plans: Start → intermediate waypoint → Start variants.
	/// </summary>
	Task<IReadOnlyList<RouteCandidate>> GenerateAsync(RoutePlan plan, CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates return leg candidates for a <see cref="ReturnStrategy.DifferentRoute"/> loop.
	/// Direction: Start → (different intermediate offset) → Start.
	/// The outbound bounding box is excluded to force a different road set.
	/// </summary>
	Task<IReadOnlyList<RouteCandidate>> GenerateReturnAsync(RoutePlan plan, ValhallaRouteResponse outboundResponse, CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates return leg candidates for a point-to-point round trip (RoundTrip = true).
	/// Direction: End → Start (exact reversal of the outbound journey).
	/// The outbound (Start → End) road corridor is excluded via Valhalla exclude_polygons
	/// to guarantee a genuinely different set of roads on the return.
	/// </summary>
	/// <param name="plan">The same plan used for the outbound route (provides constraints and profile).</param>
	/// <param name="outboundResponse">The Valhalla response from the best outbound candidate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of return leg candidates to be scored and ranked.</returns>
	Task<IReadOnlyList<RouteCandidate>> GenerateRoundTripReturnAsync(RoutePlan plan, ValhallaRouteResponse outboundResponse, CancellationToken cancellationToken = default);
}