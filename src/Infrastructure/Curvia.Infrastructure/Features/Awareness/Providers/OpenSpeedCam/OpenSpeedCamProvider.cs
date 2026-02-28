using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Shared.ValueObjects;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Curvia.Infrastructure.Features.Awareness.Providers.OpenSpeedCam;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches average-speed (section-control) cameras from OpenStreetMap
///              via the Overpass API.
///
///              Previously this provider consumed the OpenSpeedCam CSV feed
///              (https://openspeedcam.org/speedcameras.csv) which is now defunct.
///
///              It now queries Overpass for nodes and ways tagged
///              enforcement=maxspeed — the OSM convention for average/section speed
///              cameras and multi-point enforcement zones. This is complementary to
///              OsmSpeedCameraProvider, which queries highway=speed_camera (fixed
///              point cameras). Together the two providers give full coverage of both
///              camera types; the sync handler deduplicates by ExternalId.
///
///              Overpass query strategy:
///                node[enforcement=maxspeed](bbox);
///                way[enforcement=maxspeed](bbox);
///              For ways, the centre point (first node's lat/lon) is used since EF
///              stores a single coordinate per camera.
///
///              ExternalId format: "osc:{osmType}:{osmId}"
///              e.g.  "osc:node:123456"  /  "osc:way:789012"
///
///              The result is cached in memory for the lifetime of the scoped instance
///              (one Hangfire job execution) — the full Overpass response is fetched
///              once per country and reused across any repeated calls within the same scope.
/// </summary>
public sealed class OpenSpeedCamProvider : ISpeedCameraDataProvider
{
	#region Constants
	private const string OverpassEndpoint = "https://overpass-api.de/api/interpreter";
	private const int OverpassTimeoutSeconds = 60;
	#endregion

	#region Fields
	private readonly HttpClient _httpClient;
	private readonly ILogger<OpenSpeedCamProvider> _logger;

	// Per-country in-memory cache — avoids double-fetching within a single sync run
	private readonly Dictionary<string, IReadOnlyList<SpeedCameraRecord>> _cache = new(StringComparer.OrdinalIgnoreCase);

	public string ProviderName => "osm-enforcement";
	#endregion

	#region Constructor
	public OpenSpeedCamProvider(HttpClient httpClient, ILogger<OpenSpeedCamProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region ISpeedCameraDataProvider
	public async Task<IReadOnlyList<SpeedCameraRecord>> FetchAsync(
		string countryCode, CancellationToken ct = default)
	{
		if (!CountryBoundingBoxes.TryGet(countryCode, out var bbox))
		{
			_logger.LogWarning(
				"OpenSpeedCam sync: no bounding box defined for country {CountryCode}.", countryCode);
			return Array.Empty<SpeedCameraRecord>();
		}

		if (_cache.TryGetValue(countryCode, out var cached))
			return cached;

		var result = await FetchFromOverpassAsync(countryCode, bbox, ct);
		_cache[countryCode] = result;
		return result;
	}
	#endregion

	#region Private — Overpass fetch
	private async Task<IReadOnlyList<SpeedCameraRecord>> FetchFromOverpassAsync(
		string countryCode, BoundingBox bbox, CancellationToken ct)
	{
		// Query nodes AND ways tagged enforcement=maxspeed within the country bbox.
		// out center; resolves way geometry to a single representative lat/lon.
		var overpassBbox = bbox.ToOverpassBbox(); // "south,west,north,east"
		var query =
			$"[out:json][timeout:{OverpassTimeoutSeconds}];" +
			$"(" +
			$"node[\"enforcement\"=\"maxspeed\"]({overpassBbox});" +
			$"way[\"enforcement\"=\"maxspeed\"]({overpassBbox});" +
			$");" +
			$"out center;";

		var url = $"{OverpassEndpoint}?data={Uri.EscapeDataString(query)}";

		_logger.LogInformation(
			"OpenSpeedCam: fetching enforcement=maxspeed cameras for {CountryCode} from Overpass.", countryCode);

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
				"OpenSpeedCam: Overpass API request failed for {CountryCode}.", countryCode);
			return Array.Empty<SpeedCameraRecord>();
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		var records = ParseOverpassResponse(json);

		_logger.LogInformation(
			"OpenSpeedCam: {Count} enforcement=maxspeed cameras loaded for {CountryCode}.",
			records.Count, countryCode);

		return records;
	}
	#endregion

	#region Private — Parsing
	private static List<SpeedCameraRecord> ParseOverpassResponse(string json)
	{
		using var doc = JsonDocument.Parse(json);

		if (!doc.RootElement.TryGetProperty("elements", out var elements))
			return new List<SpeedCameraRecord>();

		var result = new List<SpeedCameraRecord>();

		foreach (var element in elements.EnumerateArray())
		{
			if (!element.TryGetProperty("id", out var idEl))
				continue;

			var osmId = idEl.GetInt64().ToString();
			var type = element.TryGetProperty("type", out var typeEl)
				? typeEl.GetString() ?? "node"
				: "node";

			// Nodes have lat/lon directly; ways have a "center" object (from "out center")
			double lat, lon;
			if (type == "way")
			{
				if (!element.TryGetProperty("center", out var center)) continue;
				if (!center.TryGetProperty("lat", out var cLat) ||
					!center.TryGetProperty("lon", out var cLon)) continue;
				lat = cLat.GetDouble();
				lon = cLon.GetDouble();
			}
			else
			{
				if (!element.TryGetProperty("lat", out var latEl) ||
					!element.TryGetProperty("lon", out var lonEl)) continue;
				lat = latEl.GetDouble();
				lon = lonEl.GetDouble();
			}

			int? speedLimit = null;
			var direction = CameraDirection.Unknown;

			if (element.TryGetProperty("tags", out var tags))
			{
				// maxspeed tag: "50", "50 mph", "BE:urban", etc. — parse numeric part only
				if (tags.TryGetProperty("maxspeed", out var ms))
				{
					var raw = ms.GetString()?.Split(' ')[0];
					if (int.TryParse(raw, out var kmh) && kmh > 0)
						speedLimit = kmh;
				}

				if (tags.TryGetProperty("direction", out var dir))
					direction = ParseDirection(dir.GetString());
			}

			result.Add(new SpeedCameraRecord(
				ExternalId: $"osc:{type}:{osmId}",
				Latitude: lat,
				Longitude: lon,
				SpeedLimitKmh: speedLimit,
				Source: "osm-enforcement",
				Direction: direction));
		}

		return result;
	}

	private static CameraDirection ParseDirection(string? value)
		=> value?.ToLowerInvariant() switch
		{
			"both" => CameraDirection.Both,
			"forward" => CameraDirection.Forward,
			"backward" => CameraDirection.Backward,
			_ => CameraDirection.Unknown
		};
	#endregion
}