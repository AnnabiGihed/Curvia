using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

internal sealed class RouteCandidateGenerator : IRouteCandidateGenerator
{
	private readonly IValhallaRoutingClient _client;

	public RouteCandidateGenerator(IValhallaRoutingClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public async Task<IReadOnlyList<RouteCandidate>> GenerateAsync(RoutePlan plan, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(plan);

		var locations = BuildLocations(plan);

		// Minimal set of variants for V1 (expand later)
		var variants = new (string Name, ValhallaMotorcycleOptions Moto)[]
		{
			("balanced", new ValhallaMotorcycleOptions(
				UseHighways: !plan.Constraints.AvoidHighways,
				UseTolls: !plan.Constraints.AvoidTolls,
				HighwayFactor: 0.7,
				TopSpeedKph: null)),

			("more-scenic", new ValhallaMotorcycleOptions(
				UseHighways: false,
				UseTolls: !plan.Constraints.AvoidTolls,
				HighwayFactor: 0.3,
				TopSpeedKph: null)),

			("no-tolls", new ValhallaMotorcycleOptions(
				UseHighways: !plan.Constraints.AvoidHighways,
				UseTolls: false,
				HighwayFactor: 0.6,
				TopSpeedKph: null))
		};

		var results = new List<RouteCandidate>(variants.Length);

		foreach (var v in variants)
		{
			var req = new ValhallaRouteRequest(
				Locations: locations,
				Costing: "motorcycle",
				CostingOptions: new ValhallaCostingOptions(v.Moto),
				DirectionsOptions: new ValhallaDirectionsOptions());

			var raw = await _client.RouteAsync(req, cancellationToken);
			results.Add(new RouteCandidate(raw, v.Name));
		}

		return results;
	}

	private static IReadOnlyList<ValhallaLocation> BuildLocations(RoutePlan plan)
	{
		// Start + waypoints + end (or loop center strategy later)
		var list = new List<ValhallaLocation>();

		list.Add(new ValhallaLocation(plan.Start.Latitude, plan.Start.Longitude, "break"));

		foreach (var wp in plan.Waypoints)
			list.Add(new ValhallaLocation(wp.Location.Latitude, wp.Location.Longitude, "through"));

		if (plan.End is not null)
			list.Add(new ValhallaLocation(plan.End.Latitude, plan.End.Longitude, "break"));

		// LoopSpec: V1 handling could set end=start or generate additional points around center.
		// For now you likely already enforce End or LoopSpec in domain.
		return list;
	}
}