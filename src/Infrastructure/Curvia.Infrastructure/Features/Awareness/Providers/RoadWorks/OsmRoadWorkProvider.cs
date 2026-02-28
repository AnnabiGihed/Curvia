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
///              ExternalId format: "osm:way{id}" or "osm:node{id}"
/// </summary>
public sealed class OsmRoadWorkProvider : IRoadWorkDataProvider
{
	#region Properties
	/// <summary>Short provider identifier used as the Source on persisted aggregates.</summary>
	public string ProviderName => "osm";

	/// <summary>Empty = supports all countries (serves as universal fallback).</summary>
	public IReadOnlyList<string> SupportedCountryCodes => Array.Empty<string>();
	#endregion

	#region Fields
	/// <summary>HttpClient configured for the Overpass API.</summary>
	private readonly HttpClient _httpClient;

	/// <summary>Logger for fetch lifecycle and error events.</summary>
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
	/// Logs the error and re-throws on any network or HTTP failure so that the sync handler
	/// can mark this provider as failed rather than silently returning zero records.
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
		var query = $@"[out:json][timeout:90];
(
  way[""highway""=""construction""]({bb});
  node[""construction""]({bb});
)->.r;
.r out center body;";

		var url = $"https://overpass-api.de/api/interpreter?data={Uri.EscapeDataString(query)}";

		_logger.LogInformation("Fetching OSM road works for {CountryCode}.", countryCode);

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Overpass API request failed for road works ({CountryCode}).", countryCode);
			throw;
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseResponse(json);
	}
	#endregion

	#region Parsing
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