using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;

namespace Curvia.Infrastructure.Features.Awareness.Providers.Incidents;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 03-2026
/// Purpose     : Fetches live traffic incidents from the TomTom Traffic Incidents API v5.
///              One API key covers all of Europe — no per-country URL configuration.
///              The country bounding box is looked up from <see cref="CountryBoundingBoxes"/>
///              and passed as the bbox parameter to a single TomTom request per country.
///
///              TomTom iconCategory → IncidentType mapping:
///                1  Accident         → Accident
///                6  Jam              → Obstruction
///                7  Lane closed      → Obstruction
///                8  Road closed      → RoadClosure
///                12 Detour           → RoadClosure
///                9  Road works       → WorkInProgress
///                2  Fog              → WeatherHazard
///                3  Dangerous cond.  → WeatherHazard
///                4  Rain             → WeatherHazard
///                5  Ice              → WeatherHazard
///                10 Wind             → WeatherHazard
///                11 Flooding         → WeatherHazard
///                14 Broken down veh. → Obstruction
///                0  Unknown / other  → Other
///
///              TomTom magnitudeOfDelay → IncidentSeverity mapping:
///                1 Minor    → Low
///                2 Moderate → Medium
///                3 Major    → High
///                4 Undef.   → Medium  (used for road closures with unknown delay)
///                0 Unknown  → Unknown
///
///              HTTP failures are logged and re-thrown so SyncIncidentsCommandHandler
///              can track them as per-country provider failures (Hangfire job failure).
/// </summary>
public sealed class TomTomIncidentProvider : IIncidentFeedProvider
{
	#region Constants
	private const string BaseUrl = "https://api.tomtom.com/traffic/services/5/incidentDetails";

	// Request only the fields we actually map — keeps the response payload small.
	private const string Fields =
		"{incidents{type,geometry{type,coordinates}," +
		"properties{id,iconCategory,magnitudeOfDelay," +
		"events{description,iconCategory},startTime,endTime,from,to}}}";
	#endregion

	#region Fields
	private readonly HttpClient _httpClient;
	private readonly TomTomSyncOptions _options;
	private readonly ILogger<TomTomIncidentProvider> _logger;

	public string ProviderName => "tomtom";

	/// <summary>
	/// Returns every country registered in <see cref="CountryBoundingBoxes"/>.
	/// The handler also accepts an empty list as "all countries", but being explicit
	/// here makes the coverage visible at a glance.
	/// </summary>
	public IReadOnlyList<string> SupportedCountryCodes => CountryBoundingBoxes.AllCountryCodes;
	#endregion

	#region Constructor
	public TomTomIncidentProvider(
		HttpClient httpClient,
		IOptions<TomTomSyncOptions> options,
		ILogger<TomTomIncidentProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IIncidentFeedProvider
	/// <summary>
	/// Fires one TomTom request for the given country's bounding box and returns the parsed incidents.
	/// Returns empty when no bounding box is registered for the country.
	/// HTTP failures are logged and re-thrown.
	/// </summary>
	public async Task<IReadOnlyList<IncidentRecord>> FetchAsync(
		string countryCode, CancellationToken ct = default)
	{
		if (!CountryBoundingBoxes.TryGet(countryCode, out var bbox))
		{
			_logger.LogDebug("No bounding box registered for {CountryCode}; skipping TomTom fetch.", countryCode);
			return Array.Empty<IncidentRecord>();
		}

		// TomTom bbox format: minLon,minLat,maxLon,maxLat  (West,South,East,North)
		var bboxParam = $"{bbox.West:F6},{bbox.South:F6},{bbox.East:F6},{bbox.North:F6}";
		var url = BuildUrl(bboxParam);

		_logger.LogInformation("Fetching TomTom incidents for {CountryCode} (bbox={Bbox}).",
			countryCode, bboxParam);

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "TomTom incident request failed for {CountryCode}.", countryCode);
			throw; // re-throw so SyncIncidentsCommandHandler records this as a provider failure
		}

		var content = await response.Content.ReadAsStringAsync(ct);
		return ParseResponse(content, countryCode);
	}
	#endregion

	#region Private helpers
	private string BuildUrl(string bboxParam)
		=> $"{BaseUrl}" +
		   $"?key={Uri.EscapeDataString(_options.ApiKey)}" +
		   $"&bbox={bboxParam}" +
		   $"&fields={Uri.EscapeDataString(Fields)}" +
		   $"&language=en-GB" +
		   $"&timeValidityFilter=present";

	private IReadOnlyList<IncidentRecord> ParseResponse(string json, string countryCode)
	{
		var records = new List<IncidentRecord>();
		try
		{
			using var doc = JsonDocument.Parse(json);
			if (!doc.RootElement.TryGetProperty("incidents", out var incidents)) return records;

			foreach (var feature in incidents.EnumerateArray())
			{
				try
				{
					var record = ParseFeature(feature);
					if (record is not null) records.Add(record);
				}
				catch { /* skip malformed individual incidents */ }
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse TomTom response for {CountryCode}.", countryCode);
		}

		return records;
	}

	private static IncidentRecord? ParseFeature(JsonElement feature)
	{
		// ── Coordinates ───────────────────────────────────────────────────────────
		if (!feature.TryGetProperty("geometry", out var geometry)) return null;
		if (!geometry.TryGetProperty("coordinates", out var coords)) return null;

		var coordArr = coords.EnumerateArray().ToList();
		if (coordArr.Count < 2) return null;

		var lon = coordArr[0].GetDouble();
		var lat = coordArr[1].GetDouble();

		if (lat == 0 && lon == 0) return null;

		// ── Properties ───────────────────────────────────────────────────────────
		if (!feature.TryGetProperty("properties", out var props)) return null;

		var id = props.TryGetProperty("id", out var idProp)
			? idProp.GetString() ?? Guid.NewGuid().ToString()
			: Guid.NewGuid().ToString();

		var iconCategory = props.TryGetProperty("iconCategory", out var ic) ? ic.GetInt32() : 0;
		var magnitudeOfDelay = props.TryGetProperty("magnitudeOfDelay", out var mod) ? mod.GetInt32() : 0;

		// ── Validity window ───────────────────────────────────────────────────────
		var now = DateTime.UtcNow;
		var validFrom = now;
		var validUntil = now.AddHours(2); // sensible default when endTime is absent

		if (props.TryGetProperty("startTime", out var st)
			&& DateTime.TryParse(st.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedStart))
			validFrom = parsedStart.ToUniversalTime();

		if (props.TryGetProperty("endTime", out var et)
			&& DateTime.TryParse(et.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedEnd))
			validUntil = parsedEnd.ToUniversalTime();

		if (validUntil <= DateTime.UtcNow) return null; // already expired

		return new IncidentRecord(
			ExternalId: $"tomtom:{id}",
			Latitude: lat,
			Longitude: lon,
			IncidentType: MapIconCategory(iconCategory),
			Severity: MapMagnitudeOfDelay(magnitudeOfDelay),
			Description: ExtractDescription(props),
			ValidFromUtc: validFrom,
			ValidUntilUtc: validUntil);
	}

	// ─── Mapping helpers ─────────────────────────────────────────────────────────

	private static IncidentType MapIconCategory(int iconCategory) => iconCategory switch
	{
		1           => IncidentType.Accident,
		6 or 7 or 14 => IncidentType.Obstruction,    // jam, lane closed, broken-down vehicle
		8 or 12     => IncidentType.RoadClosure,      // road closed, detour
		9           => IncidentType.WorkInProgress,
		2 or 3 or 4 or 5 or 10 or 11 => IncidentType.WeatherHazard, // fog, dangerous, rain, ice, wind, flooding
		_           => IncidentType.Other
	};

	private static IncidentSeverity MapMagnitudeOfDelay(int magnitudeOfDelay) => magnitudeOfDelay switch
	{
		1 => IncidentSeverity.Low,
		2 => IncidentSeverity.Medium,
		3 => IncidentSeverity.High,
		4 => IncidentSeverity.Medium, // "undefined" — TomTom uses this for road closures without a measured delay
		_ => IncidentSeverity.Unknown
	};

	/// <summary>
	/// Returns the first non-empty event description, or a "from → to" fallback constructed
	/// from the road segment info, or null when neither is available.
	/// </summary>
	private static string? ExtractDescription(JsonElement props)
	{
		if (props.TryGetProperty("events", out var events))
		{
			foreach (var ev in events.EnumerateArray())
			{
				if (ev.TryGetProperty("description", out var desc))
				{
					var text = desc.GetString();
					if (!string.IsNullOrWhiteSpace(text))
						return text.Length > 500 ? text[..500] : text;
				}
			}
		}

		var from = props.TryGetProperty("from", out var f) ? f.GetString() : null;
		var to   = props.TryGetProperty("to",   out var t) ? t.GetString() : null;

		return (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to))
			? $"{from} → {to}"
			: from ?? to;
	}
	#endregion
}
