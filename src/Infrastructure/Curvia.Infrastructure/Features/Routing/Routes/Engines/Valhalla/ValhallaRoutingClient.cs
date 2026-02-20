using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Curvia.Application.Features.Routing.Routes.Contracts;

namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : HTTP client wrapping Valhalla's /route endpoint.
///              Uses snake_case JSON serialization to match Valhalla's API contract
///              (e.g. costing_options, use_highways, use_tolls).
///              Null properties are omitted from the request body.
/// </summary>
internal sealed class ValhallaRoutingClient : IValhallaRoutingClient
{
	/// <summary>
	/// .NET 10 built-in snake_case policy + null-value suppression.
	/// Applied to both serialization (request) and deserialization (response).
	/// </summary>
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true   // tolerant deserialization of responses
	};

	private readonly HttpClient _http;

	public ValhallaRoutingClient(HttpClient http, IOptions<ValhallaOptions> options)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));

		var opt = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
		_http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
	}

	public async Task<ValhallaRouteResponse> RouteAsync(ValhallaRouteRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var response = await _http.PostAsJsonAsync("route", request, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new InvalidOperationException(
				$"Valhalla returned HTTP {(int)response.StatusCode}: {body}");
		}

		var result = await response.Content.ReadFromJsonAsync<ValhallaRouteResponse>(
			JsonOptions,
			cancellationToken: cancellationToken);

		if (result is null)
			throw new InvalidOperationException("Valhalla returned an empty response body.");

		return result;
	}
}