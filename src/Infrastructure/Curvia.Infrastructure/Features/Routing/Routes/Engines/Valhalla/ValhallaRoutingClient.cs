using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Curvia.Application.Features.Routing.Routes.Contracts;

namespace Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla;

internal sealed class ValhallaRoutingClient : IValhallaRoutingClient
{
	private readonly HttpClient _http;

	public ValhallaRoutingClient(HttpClient http, IOptions<ValhallaOptions> options)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));

		var opt = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
		_http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
	}

	public async Task<ValhallaRouteResponse> RouteAsync(
		ValhallaRouteRequest request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var response = await _http.PostAsJsonAsync("route", request, cancellationToken);
		response.EnsureSuccessStatusCode();

		var body = await response.Content.ReadFromJsonAsync<ValhallaRouteResponse>(cancellationToken: cancellationToken);
		if (body is null)
			throw new InvalidOperationException("Valhalla returned an empty response body.");

		return body;
	}
}