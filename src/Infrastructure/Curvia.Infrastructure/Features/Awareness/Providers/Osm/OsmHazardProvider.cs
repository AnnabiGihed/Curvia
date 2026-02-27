using System.Text.Json;
using Microsoft.Extensions.Logging;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;

namespace Curvia.Infrastructure.Features.Awareness.Providers.Osm;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches permanent road hazards from OSM via the Overpass API.
///
///              Queried OSM tags (union of node + way):
///                - hazard=* (all hazard subtypes)
///                - railway=level_crossing
///                - ford=yes
///                - cattle_grid=yes
///                - traffic_calming=rumble_strip (treated as SlipperyRoad for motorcyclists)
///
///              ExternalId format: "osm:{type}{id}" e.g. "osm:node123456"
/// </summary>
public sealed class OsmHazardProvider : IHazardDataProvider
{
	#region Fields
	private readonly HttpClient _httpClient;
	private readonly ILogger<OsmHazardProvider> _logger;
	public string ProviderName => "osm";
	#endregion

	#region Constructor
	public OsmHazardProvider(HttpClient httpClient, ILogger<OsmHazardProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IHazardDataProvider
	public async Task<IReadOnlyList<HazardRecord>> FetchAsync(
		string countryCode, CancellationToken ct = default)
	{
		if (!CountryBoundingBoxes.TryGet(countryCode, out var bbox))
		{
			_logger.LogWarning("OSM hazard sync: no bounding box defined for {CountryCode}.", countryCode);
			return Array.Empty<HazardRecord>();
		}

		var bb = bbox.ToOverpassBbox();

		// Union query: nodes and ways matching any of the hazard tags
		var query = $@"[out:json][timeout:120];
(
  node[""hazard""]({bb});
  node[""railway""=""level_crossing""]({bb});
  node[""ford""=""yes""]({bb});
  node[""cattle_grid""=""yes""]({bb});
  node[""traffic_calming""=""rumble_strip""]({bb});
)->.result;
.result out body;";

		var url = $"https://overpass-api.de/api/interpreter?data={Uri.EscapeDataString(query)}";

		_logger.LogInformation("Fetching OSM hazards for {CountryCode}.", countryCode);

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Overpass API request failed for hazards ({CountryCode}).", countryCode);
			return Array.Empty<HazardRecord>();
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseResponse(json);
	}
	#endregion

	#region Parsing
	private static IReadOnlyList<HazardRecord> ParseResponse(string json)
	{
		using var doc = JsonDocument.Parse(json);
		if (!doc.RootElement.TryGetProperty("elements", out var elements))
			return Array.Empty<HazardRecord>();

		var result = new List<HazardRecord>();
		foreach (var element in elements.EnumerateArray())
		{
			if (!element.TryGetProperty("lat", out var latEl) ||
				!element.TryGetProperty("lon", out var lonEl) ||
				!element.TryGetProperty("id", out var idEl))
				continue;

			var lat = latEl.GetDouble();
			var lon = lonEl.GetDouble();
			var id = idEl.GetInt64().ToString();
			var externalId = $"osm:node{id}";

			element.TryGetProperty("tags", out var tags);
			var hazardType = ClassifyHazard(tags);
			var description = ExtractDescription(tags);

			result.Add(new HazardRecord(externalId, lat, lon, "osm", hazardType, description));
		}

		return result;
	}

	private static HazardType ClassifyHazard(JsonElement tags)
	{
		if (!tags.ValueKind.Equals(JsonValueKind.Object))
			return HazardType.Other;

		if (tags.TryGetProperty("railway", out var railway)
			&& railway.GetString() == "level_crossing")
			return HazardType.LevelCrossing;

		if (tags.TryGetProperty("ford", out var ford)
			&& ford.GetString() == "yes")
			return HazardType.Ford;

		if (tags.TryGetProperty("cattle_grid", out var cg)
			&& cg.GetString() == "yes")
			return HazardType.CattleGrid;

		if (tags.TryGetProperty("traffic_calming", out var tc)
			&& tc.GetString() == "rumble_strip")
			return HazardType.SlipperyRoad;

		if (tags.TryGetProperty("hazard", out var hazard))
		{
			return hazard.GetString()?.ToLowerInvariant() switch
			{
				"slippery_road" or "slippery" => HazardType.SlipperyRoad,
				"road_damage" or "pot_hole" => HazardType.RoadDamage,
				"narrow_road" or "narrow" => HazardType.NarrowRoad,
				"steep_descent" or "steep" => HazardType.SteepDescent,
				"sharp_bend" or "sharp_curve" => HazardType.SharpBend,
				"flooding" or "flood_prone" => HazardType.FloodingProne,
				"falling_rocks" or "rock_fall" => HazardType.FallingRocks,
				_ => HazardType.Other
			};
		}

		return HazardType.Other;
	}

	private static string? ExtractDescription(JsonElement tags)
	{
		if (!tags.ValueKind.Equals(JsonValueKind.Object))
			return null;

		if (tags.TryGetProperty("description", out var desc))
			return desc.GetString();

		if (tags.TryGetProperty("name", out var name))
			return name.GetString();

		return null;
	}
	#endregion
}