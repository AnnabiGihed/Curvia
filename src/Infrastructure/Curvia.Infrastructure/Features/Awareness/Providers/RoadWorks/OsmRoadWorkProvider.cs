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
	#region Fields
	private readonly HttpClient _httpClient;
	private readonly ILogger<OsmRoadWorkProvider> _logger;

	public string ProviderName => "osm";

	/// <summary>
	/// Empty = supports all countries (serves as universal fallback).
	/// Country-specific providers take priority via the resolver.
	/// </summary>
	public IReadOnlyList<string> SupportedCountryCodes => Array.Empty<string>();
	#endregion

	#region Constructor
	public OsmRoadWorkProvider(HttpClient httpClient, ILogger<OsmRoadWorkProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IRoadWorkDataProvider
	public async Task<IReadOnlyList<RoadWorkRecord>> FetchAsync(
		string countryCode, CancellationToken ct = default)
	{
		if (!CountryBoundingBoxes.TryGet(countryCode, out var bbox))
		{
			_logger.LogWarning("OSM road works sync: no bounding box for {CountryCode}.", countryCode);
			return Array.Empty<RoadWorkRecord>();
		}

		var bb = bbox.ToOverpassBbox();

		var query = $@"[out:json][timeout:90];
(
  way[""highway""=""construction""]({bb});
  node[""construction""]({bb});
)->.r;
.r out body;";

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
			return Array.Empty<RoadWorkRecord>();
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseResponse(json);
	}
	#endregion

	#region Parsing
	private static IReadOnlyList<RoadWorkRecord> ParseResponse(string json)
	{
		using var doc = JsonDocument.Parse(json);
		if (!doc.RootElement.TryGetProperty("elements", out var elements))
			return Array.Empty<RoadWorkRecord>();

		var result = new List<RoadWorkRecord>();
		foreach (var element in elements.EnumerateArray())
		{
			if (!element.TryGetProperty("id", out var idEl)) continue;
			var id = idEl.GetInt64().ToString();
			var type = element.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "node";

			double lat, lon;
			if (type == "way")
			{
				// Ways don't have direct lat/lon — use center if provided by Overpass
				if (!element.TryGetProperty("center", out var center)) continue;
				lat = center.GetProperty("lat").GetDouble();
				lon = center.GetProperty("lon").GetDouble();
			}
			else
			{
				if (!element.TryGetProperty("lat", out var latEl) ||
					!element.TryGetProperty("lon", out var lonEl)) continue;
				lat = latEl.GetDouble();
				lon = lonEl.GetDouble();
			}

			element.TryGetProperty("tags", out var tags);

			var title = ExtractTitle(tags);

			// Parse date tags — construction:date, date:open, expected_dsn
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

	private static string ExtractTitle(JsonElement tags)
	{
		if (tags.ValueKind != JsonValueKind.Object) return "Road works";

		if (tags.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
			return name.GetString()!;

		if (tags.TryGetProperty("description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
		{
			var d = desc.GetString()!;
			return d.Length > 200 ? d[..200] : d;
		}

		return "Road works";
	}

	private static (DateTime From, DateTime Until) ExtractDates(JsonElement tags)
	{
		var now = DateTime.UtcNow;
		var from = now;
		var until = now.AddDays(30); // conservative default

		if (tags.ValueKind != JsonValueKind.Object) return (from, until);

		// "date:open" = expected reopening date
		if (tags.TryGetProperty("date:open", out var openDate)
			&& DateTime.TryParse(openDate.GetString(), out var parsedOpen))
			until = parsedOpen.ToUniversalTime();

		// "construction:date" = start date
		if (tags.TryGetProperty("construction:date", out var startDate)
			&& DateTime.TryParse(startDate.GetString(), out var parsedStart))
			from = parsedStart.ToUniversalTime();

		return (from, until);
	}
	#endregion
}