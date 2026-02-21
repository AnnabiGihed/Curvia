using Curvia.Application.Features.Routing.Routes.Contracts.Services.Scoring;

namespace Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.HeightClient;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for Valhalla's /height endpoint.
///              Returns elevation data in meters for each point along a route shape.
///              Used by <see cref="IRouteScoringService"/> to compute real elevation
///              gain and variance scores without requiring a separate graph worker.
/// </summary>
public interface IValhallaHeightClient
{
	/// <summary>
	/// Retrieves elevation values in meters for each point in the given shape.
	/// </summary>
	/// <param name="request">Shape points to query elevation for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Elevation response containing one height per shape point.</returns>
	Task<ValhallaHeightResponse> GetHeightsAsync(ValhallaHeightRequest request,	CancellationToken cancellationToken = default);
}