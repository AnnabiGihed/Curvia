using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Curvia.Infrastructure.Features.Awareness.Providers.RoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches road works from OSM via Overpass as a fallback for countries
///              that do not have a dedicated authoritative road works feed.
///
///              Queries: nodes/ways tagged highway=construction or construction=*
///              with an associated date:open or expected_dsn tag.
///
///              Limitation: OSM road works are community-maintained and tend to cover
///              only significant or long-running works. Short-term local works are
///              typically not present. This is the best available free source for
///              countries outside GIPOD coverage.
///
///              Validity period: if no date tags are present, a 30-day window from
///              the current date is assumed as a conservative estimate.
///
///              Resilience: the public Overpass API (overpass-api.de) can return 504
///              under heavy load. Up to <see cref="MaxAttempts"/> attempts are made with
///              exponential back-off. If all attempts against the primary endpoint fail,
///              the query is retried against a secondary mirror endpoint before giving up.
///
///              ExternalId format: "osm:way{id}" or "osm:node{id}"
/// </summary>
public sealed class OsmRoadWorkProvider : IRoadWorkDataProvider
{
	#region Constants
	/// <summary>Primary Overpass API endpoint.</summary>
	private const string PrimaryEndpoint = "https://overpass-api.de/api/interpreter";

	/// <summary>
	/// Secondary Overpass mirror used when the primary endpoint returns repeated 504s.
	/// overpass.kumi.systems is a well-maintained public mirror.
	/// </summary>
	private const string FallbackEndpoint = "https://overpass.kumi.systems/api/interpreter";

	/// <summary>Number of attempts per endpoint before falling over to the next.</summary>
	private const int MaxAttempts = 3;

	/// <summary>Base delay in milliseconds for exponential back-off between retries.</summary>
	private const int BaseRetryDelayMs = 2000;
	#endregion

	#region Properties
	/// <summary>Short provider identifier used as the Source on persisted aggregates.</summary>
	public string ProviderName => "osm";

	/// <summary>Empty = supports all countries (serves as universal fallback).</summary>
	public IReadOnlyList<string> SupportedCountryCodes => Array.Empty<string>();
	#endregion

	#region Fields
	/// <summary>HttpClient configured for the Overpass API.</summary>
	private readonly HttpClient _httpClient;

	/// <summary>Logger for fetch lifecycle events.</summary>
	private readonly ILogger<OsmRoadWorkProvider> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="OsmRoadWorkProvider"/>.
	/// </summary>
	/// <param name="httpClient">HttpClient configured for the Overpass API.</param>
	/// <param name="logger">Logger instance.</param>
	public OsmRoadWorkProvider(HttpClient httpClient, ILogger<OsmRoadWorkProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IRoadWorkDataProvider
	/// <summary>
	/// Fetches road works from the Overpass API for the given country.
	/// Attempts the primary endpoint up to <see cref="MaxAttempts"/> times with exponential
	/// back-off, then falls over to the secondary mirror if all primary attempts fail.
	/// Re-throws if all endpoints and all attempts are exhausted — error logging is owned
	/// by the caller's resilience layer which has full provider + country context.
	/// </summary>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Parsed road-work records from OSM.</returns>
	public async Task<IReadOnlyList<RoadWorkRecord>> FetchAsync(string countryCode, CancellationToken ct = default)
	{
		if (!CountryBoundingBoxes.TryGet(countryCode, out var bbox))
		{
			_logger.LogWarning("OSM road works sync: no bounding box for {CountryCode}.", countryCode);
			return Array.Empty<RoadWorkRecord>();
		}

		var bb = bbox.ToOverpassBbox();

		// "out center body" is required so that ways receive a "center" property
		// with their centroid lat/lon — without it ways silently have no coordinates
		// and are dropped by the parser.
		var query = $@"[out:json][timeout:120];
(
  way[""highway""=""construction""]({bb});
  node[""construction""]({bb});
)->.r;
.r out center body;";

		_logger.LogInformation("Fetching OSM road works for {CountryCode}.", countryCode);

		var endpoints = new[] { PrimaryEndpoint, FallbackEndpoint };

		foreach (var endpoint in endpoints)
		{
			var result = await TryFetchFromEndpointAsync(endpoint, query, countryCode, ct);
			if (result is not null)
				return result;
		}

		// All endpoints and all attempts exhausted — let the exception propagate so
		// FetchFromApplicableProvidersAsync marks this provider as failed.
		throw new HttpRequestException($"OSM road works fetch failed for {countryCode}: all Overpass endpoints exhausted after {MaxAttempts} attempts each.");
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Attempts to fetch road works from a single Overpass endpoint with exponential back-off.
	/// Returns null when all attempts for this endpoint fail, so the caller can try the next endpoint.
	/// </summary>
	/// <param name="endpoint">Overpass API base URL (primary or mirror).</param>
	/// <param name="query">Overpass QL query string.</param>
	/// <param name="countryCode">Country code used for log context only.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Parsed records on success, or null if all attempts against this endpoint failed.</returns>
	private async Task<IReadOnlyList<RoadWorkRecord>?> TryFetchFromEndpointAsync(string endpoint, string query, string countryCode, CancellationToken ct)
	{
		var url = $"{endpoint}?data={Uri.EscapeDataString(query)}";

		for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			try
			{
				var response = await _httpClient.GetAsync(url, ct);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync(ct);
				return ParseResponse(json);
			}
			catch (HttpRequestException ex) when (IsTransient(ex))
			{
				if (attempt == MaxAttempts)
				{
					_logger.LogWarning("OSM road works for {CountryCode}: endpoint {Endpoint} failed after {Attempts} attempts ({Error}). Trying next endpoint.",
						countryCode, endpoint, MaxAttempts, ex.Message);
					return null;
				}

				var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt - 1); // 2s, 4s, 8s
				_logger.LogWarning("OSM road works for {CountryCode}: attempt {Attempt}/{Max} against {Endpoint} failed ({Error}). Retrying in {Delay}ms.",
					countryCode, attempt, MaxAttempts, endpoint, ex.Message, delay);

				await Task.Delay(delay, ct);
			}
		}

		return null;
	}

	/// <summary>
	/// Returns true for HTTP status codes that represent a transient server-side failure
	/// and are worth retrying: 429 Too Many Requests, 503 Service Unavailable, 504 Gateway Timeout.
	/// </summary>
	/// <param name="ex">The HttpRequestException to inspect.</param>
	/// <returns>True when the error is considered transient.</returns>
	private static bool IsTransient(HttpRequestException ex)
	{
		return ex.StatusCode is System.Net.HttpStatusCode.TooManyRequests
			or System.Net.HttpStatusCode.ServiceUnavailable
			or System.Net.HttpStatusCode.GatewayTimeout;
	}

	/// <summary>
	/// Parses the Overpass JSON response into road-work records.
	/// </summary>
	/// <param name="json">Raw JSON string from the Overpass response body.</param>
	/// <returns>Parsed road-work records. Empty list when no valid elements are found.</returns>
	private static IReadOnlyList<RoadWorkRecord> ParseResponse(string json)
	{
		using var doc = JsonDocument.Parse(json);
		if (!doc.RootElement.TryGetProperty("elements", out var elements))
			return Array.Empty<RoadWorkRecord>();

		var result = new List<RoadWorkRecord>();
		foreach (var element in elements.EnumerateArray())
		{
			if (!element.TryGetProperty("id", out var idEl))
				continue;
			var id = idEl.GetInt64().ToString();
			var type = element.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "node";

			double lat, lon;
			if (type == "way")
			{
				if (!element.TryGetProperty("center", out var center))
					continue;
				lat = center.GetProperty("lat").GetDouble();
				lon = center.GetProperty("lon").GetDouble();
			}
			else
			{
				if (!element.TryGetProperty("lat", out var latEl) || !element.TryGetProperty("lon", out var lonEl))
					continue;
				lat = latEl.GetDouble();
				lon = lonEl.GetDouble();
			}

			element.TryGetProperty("tags", out var tags);

			var title = ExtractTitle(tags);
			var (validFrom, validUntil) = ExtractDates(tags);

			result.Add(new RoadWorkRecord(
				ExternalId: $"osm:{type}{id}",
				Latitude: lat,
				Longitude: lon,
				Title: title,
				Description: null,
				ValidFromUtc: validFrom,
				ValidUntilUtc: validUntil));
		}

		return result;
	}

	/// <summary>
	/// Extracts a human-readable title from OSM tags, falling back to "Road works".
	/// </summary>
	/// <param name="tags">The tags element from an OSM feature.</param>
	/// <returns>A title string no longer than 200 characters.</returns>
	private static string ExtractTitle(JsonElement tags)
	{
		if (tags.ValueKind != JsonValueKind.Object)
			return "Road works";

		if (tags.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
			return name.GetString()!;

		if (tags.TryGetProperty("description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
		{
			var d = desc.GetString()!;
			return d.Length > 200 ? d[..200] : d;
		}

		return "Road works";
	}

	/// <summary>
	/// Extracts the validity window from OSM date tags.
	/// Falls back to today through +30 days when no date tags are present.
	/// </summary>
	/// <param name="tags">The tags element from an OSM feature.</param>
	/// <returns>A tuple of (ValidFromUtc, ValidUntilUtc).</returns>
	private static (DateTime ValidFrom, DateTime ValidUntil) ExtractDates(JsonElement tags)
	{
		var validFrom = DateTime.UtcNow;
		var validUntil = DateTime.UtcNow.AddDays(30);

		if (tags.ValueKind != JsonValueKind.Object)
			return (validFrom, validUntil);

		if (tags.TryGetProperty("construction:date", out var startEl) && DateTime.TryParse(startEl.GetString(), out var parsedStart))
			validFrom = parsedStart.ToUniversalTime();

		if (tags.TryGetProperty("date:open", out var endEl) && DateTime.TryParse(endEl.GetString(), out var parsedEnd))
			validUntil = parsedEnd.ToUniversalTime();
		else if (tags.TryGetProperty("expected_dsn", out var dsnEl) && DateTime.TryParse(dsnEl.GetString(), out var parsedDsn))
			validUntil = parsedDsn.ToUniversalTime();

		return (validFrom, validUntil);
	}
	#endregion
}