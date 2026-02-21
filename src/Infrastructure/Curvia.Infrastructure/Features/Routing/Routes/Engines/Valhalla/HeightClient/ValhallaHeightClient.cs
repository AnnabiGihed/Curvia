using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.Models;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.RoutingClient;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.HeightClient;


namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.HeightClient;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : HTTP client wrapper for Valhalla's /height endpoint.
///              Returns elevation data (meters, WGS84) for a list of shape points.
///              Uses the same snake_case serialization settings as <see cref="ValhallaRoutingClient"/>.
/// </summary>
internal sealed class ValhallaHeightClient : IValhallaHeightClient
{
	#region Fields
	private readonly HttpClient _http;

	/// <summary>
	/// Maximum number of shape points sent per request.
	/// Valhalla has a default limit of 750,000 points for the skadi (height) service.
	/// We cap at 500 to keep requests fast and avoid memory spikes.
	/// </summary>
	private const int MaxPointsPerRequest = 500;

	/// <summary>
	/// Shared JSON options: snake_case naming, null-value exclusion, case-insensitive deserialization.
	/// Must match the options used by <see cref="ValhallaRoutingClient"/>.
	/// </summary>
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises the client with the injected HttpClient and Valhalla options.
	/// </summary>
	/// <param name="http">Injected <see cref="HttpClient"/> instance.</param>
	/// <param name="options">Valhalla connection options (BaseUrl, TimeoutSeconds).</param>
	public ValhallaHeightClient(HttpClient http, IOptions<ValhallaOptions> options)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));

		var opt = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
		_http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
	}
	#endregion

	#region IValhallaHeightClient
	/// <inheritdoc/>
	public async Task<ValhallaHeightResponse> GetHeightsAsync(ValhallaHeightRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		// Sample the shape to stay within MaxPointsPerRequest
		var sampledShape = SamplePoints(request.Shape, MaxPointsPerRequest);

		var sampledRequest = request with { Shape = sampledShape };

		var httpResponse = await _http.PostAsJsonAsync("height", sampledRequest, JsonOptions, cancellationToken);
		httpResponse.EnsureSuccessStatusCode();

		var body = await httpResponse.Content
			.ReadFromJsonAsync<ValhallaHeightResponse>(JsonOptions, cancellationToken);

		if (body is null)
			throw new InvalidOperationException("Valhalla returned an empty response body for /height.");

		return body;
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Samples the input point list to at most <paramref name="maxPoints"/> entries
	/// using uniform striding, always preserving the first and last point.
	/// </summary>
	private static IReadOnlyList<ValhallaLocation> SamplePoints(IReadOnlyList<ValhallaLocation> points, int maxPoints)
	{
		if (points.Count <= maxPoints)
			return points;

		var result = new List<ValhallaLocation>(maxPoints);
		var stride = (double)(points.Count - 1) / (maxPoints - 1);

		for (var i = 0; i < maxPoints; i++)
		{
			var index = (int)Math.Round(i * stride);
			result.Add(points[Math.Min(index, points.Count - 1)]);
		}

		return result;
	}
	#endregion
}