using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Application.Features.Routing.Routes.Contracts;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Calls Valhalla with 3 route variants and returns the raw responses
///              for downstream scoring and selection.
///
///              Variants:
///              - balanced     : normal highway/toll usage, moderate fun preference
///              - more-scenic  : avoids highways strongly, tolerates tolls
///              - no-tolls     : normal highway usage, zero toll preference
///
///              Constraint flags (AvoidHighways, AvoidTolls) from the RoutePlan
///              are applied as hard overrides across all variants.
/// </summary>
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

		// Hard constraint overrides: when the rider explicitly avoids highways/tolls,
		// set the corresponding Valhalla preference to 0 regardless of variant.
		var hardHighwaysCap = plan.Constraints.AvoidHighways ? 0.0 : (double?)null;
		var hardTollsCap = plan.Constraints.AvoidTolls ? 0.0 : (double?)null;

		var variants = new (string Name, double UseHighways, double UseTolls)[]
		{
			("balanced",    hardHighwaysCap ?? 0.7, hardTollsCap ?? 0.9),
			("more-scenic", hardHighwaysCap ?? 0.2, hardTollsCap ?? 0.5),
			("no-tolls",    hardHighwaysCap ?? 0.7, 0.0),
		};

		var results = new List<RouteCandidate>(variants.Length);

		foreach (var v in variants)
		{
			var motoOptions = new ValhallaMotorcycleOptions(
				UseHighways: v.UseHighways,
				UseTolls: v.UseTolls,
				TopSpeed: null);

			var req = new ValhallaRouteRequest(
				Locations: locations,
				Costing: "motorcycle",
				CostingOptions: new ValhallaCostingOptions(motoOptions),
				DirectionsOptions: new ValhallaDirectionsOptions());

			var raw = await _client.RouteAsync(req, cancellationToken);
			results.Add(new RouteCandidate(raw, v.Name));
		}

		return results;
	}

	private static IReadOnlyList<ValhallaLocation> BuildLocations(RoutePlan plan)
	{
		var list = new List<ValhallaLocation>();

		list.Add(new ValhallaLocation(plan.Start.Latitude, plan.Start.Longitude, "break"));

		foreach (var wp in plan.Waypoints)
			list.Add(new ValhallaLocation(wp.Location.Latitude, wp.Location.Longitude, "through"));

		if (plan.End is not null)
			list.Add(new ValhallaLocation(plan.End.Latitude, plan.End.Longitude, "break"));

		// Loop mode: Valhalla receives only the start. The end = start will be enforced
		// by the LoopSpec logic once the worker pipeline generates waypoints around the loop.
		// For V1, a single location causes Valhalla to do an isochrone-style open route.
		// TODO: Generate intermediate loop waypoints using LoopSpec.TargetDistance and bearing.

		return list;
	}
}