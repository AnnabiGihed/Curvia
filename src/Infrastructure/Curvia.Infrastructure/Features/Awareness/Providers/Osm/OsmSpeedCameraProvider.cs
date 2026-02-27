using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Domain.Features.Awareness.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Curvia.Infrastructure.Features.Awareness.Providers.Osm;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches speed cameras from OpenStreetMap via the Overpass API.
///              Queries all nodes tagged highway=speed_camera within the country's
///              bounding box.
///
///              Country bounding boxes are approximate WGS84 extents sufficient
///              for Overpass queries. They do not need to be precise.
///
///              Public Overpass instances (overpass-api.de, overpass.kumi.systems)
///              are used with a 1-second inter-request delay to avoid being rate-limited.
///              In production, self-hosting an Overpass instance is recommended for
///              pan-European sync runs.
///
///              ExternalId format: "osm:{nodeId}"
/// </summary>
public sealed class OsmSpeedCameraProvider : ISpeedCameraDataProvider
{
	#region Fields
	private readonly HttpClient _httpClient;
	private readonly ILogger<OsmSpeedCameraProvider> _logger;
	public string ProviderName => "osm";
	#endregion

	#region Constructor
	public OsmSpeedCameraProvider(HttpClient httpClient, ILogger<OsmSpeedCameraProvider> logger)
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
			_logger.LogWarning("OSM speed camera sync: no bounding box defined for country {CountryCode}.", countryCode);
			return Array.Empty<SpeedCameraRecord>();
		}

		// Overpass QL: all speed camera nodes within the bounding box
		var query = $"[out:json][timeout:60];node[\"highway\"=\"speed_camera\"]({bbox.ToOverpassBbox()});out body;";
		var url = $"https://overpass-api.de/api/interpreter?data={Uri.EscapeDataString(query)}";

		_logger.LogInformation("Fetching OSM speed cameras for {CountryCode} from Overpass.", countryCode);

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Overpass API request failed for {CountryCode}.", countryCode);
			return Array.Empty<SpeedCameraRecord>();
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseOverpassResponse(json);
	}
	#endregion

	#region Parsing
	private static IReadOnlyList<SpeedCameraRecord> ParseOverpassResponse(string json)
	{
		using var doc = JsonDocument.Parse(json);
		if (!doc.RootElement.TryGetProperty("elements", out var elements))
			return Array.Empty<SpeedCameraRecord>();

		var result = new List<SpeedCameraRecord>();
		foreach (var element in elements.EnumerateArray())
		{
			if (!element.TryGetProperty("lat", out var latEl) ||
				!element.TryGetProperty("lon", out var lonEl) ||
				!element.TryGetProperty("id", out var idEl))
				continue;

			var lat = latEl.GetDouble();
			var lon = lonEl.GetDouble();
			var id = idEl.GetInt64().ToString();

			int? speedLimit = null;
			var direction = CameraDirection.Unknown;

			if (element.TryGetProperty("tags", out var tags))
			{
				if (tags.TryGetProperty("maxspeed", out var ms)
					&& int.TryParse(ms.GetString()?.Split(' ')[0], out var kmh))
					speedLimit = kmh;

				if (tags.TryGetProperty("direction", out var dir))
					direction = ParseDirection(dir.GetString());
			}

			result.Add(new SpeedCameraRecord(
				ExternalId: $"osm:{id}",
				Latitude: lat,
				Longitude: lon,
				SpeedLimitKmh: speedLimit,
				Source: "osm",
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