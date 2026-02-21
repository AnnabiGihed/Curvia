using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.CandidateGenerator;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port (application-layer interface) for generating Valhalla route candidates.
///              <see cref="GenerateAsync"/> produces the standard set of outbound variants.
///              <see cref="GenerateReturnAsync"/> produces return leg variants for DifferentRoute loops,
///              using the outbound response to build an exclusion zone.
/// </summary>
public interface IRouteCandidateGeneratorService
{
	/// <summary>
	/// Generates a ranked list of route candidates for the given <see cref="RoutePlan"/>.
	/// For point-to-point plans: produces start → end variants.
	/// For loop plans: produces circular start → ... → start variants.
	/// </summary>
	/// <param name="plan">The validated routing plan containing constraints and scoring profile.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of route candidates ordered by generation (scoring happens in the handler).</returns>
	Task<IReadOnlyList<RouteCandidate>> GenerateAsync(RoutePlan plan, CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates return leg candidates for a <see cref="ReturnStrategy.DifferentRoute"/> loop.
	/// The outbound route response is used to build an exclusion polygon that forces Valhalla
	/// to find a genuinely different set of roads for the return journey.
	/// </summary>
	/// <param name="plan">The same plan used for the outbound route.</param>
	/// <param name="outboundResponse">The Valhalla response from the chosen outbound candidate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of return leg candidates.</returns>
	Task<IReadOnlyList<RouteCandidate>> GenerateReturnAsync(RoutePlan plan, ValhallaRouteResponse outboundResponse, CancellationToken cancellationToken = default);
}