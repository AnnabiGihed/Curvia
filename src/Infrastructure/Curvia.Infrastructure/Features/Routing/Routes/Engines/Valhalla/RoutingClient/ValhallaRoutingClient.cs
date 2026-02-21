using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Curvia.Application.Features.Routing.Routes.Contracts.Engines.Valhalla.RoutingClient;

namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla.RoutingClient;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : HTTP client wrapper for Valhalla's /route endpoint.
///              Handles snake_case JSON serialization (required by Valhalla API),
///              null-value exclusion and case-insensitive response deserialization.
/// </summary>
internal sealed class ValhallaRoutingClient : IValhallaRoutingClient
{
	#region Fields
	private readonly HttpClient _http;

	/// <summary>
	/// Shared serializer options used for both request serialization and response deserialization.
	/// snake_case naming is required by the Valhalla REST API.
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
	/// Initialises the client and configures the base address and timeout from options.
	/// </summary>
	/// <param name="http">Injected <see cref="HttpClient"/> instance.</param>
	/// <param name="options">Valhalla connection options (BaseUrl, TimeoutSeconds).</param>
	public ValhallaRoutingClient(HttpClient http, IOptions<ValhallaOptions> options)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));
		var opt = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
		_http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
	}
	#endregion

	#region IValhallaRoutingClient
	/// <inheritdoc/>
	public async Task<ValhallaRouteResponse> RouteAsync(ValhallaRouteRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var httpResponse = await _http.PostAsJsonAsync("route", request, JsonOptions, cancellationToken);

		httpResponse.EnsureSuccessStatusCode();

		var body = await httpResponse.Content
			.ReadFromJsonAsync<ValhallaRouteResponse>(JsonOptions, cancellationToken);

		if (body is null)
			throw new InvalidOperationException("Valhalla returned an empty response body for /route.");

		return body;
	}
	#endregion
}