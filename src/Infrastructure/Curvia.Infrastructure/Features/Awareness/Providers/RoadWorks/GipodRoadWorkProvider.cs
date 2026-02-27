using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Curvia.Infrastructure.Features.Awareness.Providers.RoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches active road works from the Belgian GIPOD API.
///              GIPOD (Generiek Informatieplatform Openbaar Domein) is the government-
///              mandated registration platform for all public domain occupations in Belgium.
///              Registration is legally required before starting works — coverage is
///              authoritative for Flanders and Brussels; Wallonia coverage is improving.
///
///              API: https://api.gipod.be/api/v1/manifestations
///              Free. No API key required. GeoJSON response.
///              Paginated with limit/offset query parameters.
///
///              ExternalId format: "gipod:{id}"
/// </summary>
public sealed class GipodRoadWorkProvider : IRoadWorkDataProvider
{
	#region Constants
	private const string BaseUrl = "https://api.gipod.be/api/v1/manifestations";
	private const int PageSize = 500;
	#endregion

	#region Fields
	private readonly HttpClient _httpClient;
	private readonly ILogger<GipodRoadWorkProvider> _logger;
	public string ProviderName => "gipod";
	public IReadOnlyList<string> SupportedCountryCodes => new[] { "BE" };
	#endregion

	#region Constructor
	public GipodRoadWorkProvider(HttpClient httpClient, ILogger<GipodRoadWorkProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IRoadWorkDataProvider
	public async Task<IReadOnlyList<RoadWorkRecord>> FetchAsync(
		string countryCode, CancellationToken ct = default)
	{
		if (!countryCode.Equals("BE", StringComparison.OrdinalIgnoreCase))
			return Array.Empty<RoadWorkRecord>();

		_logger.LogInformation("Fetching GIPOD road works.");

		var result = new List<RoadWorkRecord>();
		var offset = 0;

		while (true)
		{
			var url = $"{BaseUrl}?status=ACTIVE&limit={PageSize}&offset={offset}";
			var page = await FetchPageAsync(url, ct);
			if (page is null || page.Count == 0) break;

			result.AddRange(page);
			if (page.Count < PageSize) break;
			offset += PageSize;
		}

		_logger.LogInformation("GIPOD: fetched {Count} road works.", result.Count);
		return result;
	}
	#endregion

	#region Private helpers
	private async Task<List<RoadWorkRecord>?> FetchPageAsync(string url, CancellationToken ct)
	{
		HttpResponseMessage response;
		try
		{
			response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GIPOD API request failed: {Url}.", url);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseGipodResponse(json);
	}

	private static List<RoadWorkRecord> ParseGipodResponse(string json)
	{
		var records = new List<RoadWorkRecord>();

		using var doc = JsonDocument.Parse(json);

		// GIPOD returns a GeoJSON FeatureCollection
		if (!doc.RootElement.TryGetProperty("features", out var features))
			return records;

		foreach (var feature in features.EnumerateArray())
		{
			try
			{
				var props = feature.GetProperty("properties");
				var geom = feature.GetProperty("geometry");

				// Extract centroid from geometry — GIPOD uses Polygon/MultiPolygon
				if (!TryExtractCentroid(geom, out var lat, out var lon))
					continue;

				var id = props.TryGetProperty("id", out var idEl)
					? idEl.GetRawText().Trim('"')
					: Guid.NewGuid().ToString();

				var title = props.TryGetProperty("description", out var descEl)
					? descEl.GetString() ?? "Road works"
					: "Road works";

				var validFrom = DateTime.UtcNow;
				var validUntil = DateTime.UtcNow.AddDays(30); // fallback

				if (props.TryGetProperty("startDateTime", out var startEl)
					&& DateTime.TryParse(startEl.GetString(), out var parsedStart))
					validFrom = parsedStart.ToUniversalTime();

				if (props.TryGetProperty("endDateTime", out var endEl)
					&& DateTime.TryParse(endEl.GetString(), out var parsedEnd))
					validUntil = parsedEnd.ToUniversalTime();

				if (validUntil <= DateTime.UtcNow)
					continue; // already expired

				records.Add(new RoadWorkRecord(
					ExternalId: $"gipod:{id}",
					Latitude: lat,
					Longitude: lon,
					Title: title.Length > 200 ? title[..200] : title,
					Description: null,
					ValidFromUtc: validFrom,
					ValidUntilUtc: validUntil));
			}
			catch
			{
				// Skip malformed features — don't abort the entire sync
			}
		}

		return records;
	}

	private static bool TryExtractCentroid(JsonElement geom, out double lat, out double lon)
	{
		lat = lon = 0;

		if (!geom.TryGetProperty("type", out var typeEl)) return false;
		var type = typeEl.GetString();

		if (!geom.TryGetProperty("coordinates", out var coords)) return false;

		try
		{
			switch (type)
			{
				case "Point":
					lon = coords[0].GetDouble();
					lat = coords[1].GetDouble();
					return true;

				case "Polygon":
					// Centroid of the outer ring — simple average of first ring vertices
					return RingCentroid(coords[0], out lat, out lon);

				case "MultiPolygon":
					// Centroid of the first polygon's outer ring
					return RingCentroid(coords[0][0], out lat, out lon);
			}
		}
		catch { }

		return false;
	}

	private static bool RingCentroid(JsonElement ring, out double lat, out double lon)
	{
		lat = lon = 0;
		var count = 0;
		foreach (var point in ring.EnumerateArray())
		{
			lon += point[0].GetDouble();
			lat += point[1].GetDouble();
			count++;
		}
		if (count == 0) return false;
		lat /= count;
		lon /= count;
		return true;
	}
	#endregion
}