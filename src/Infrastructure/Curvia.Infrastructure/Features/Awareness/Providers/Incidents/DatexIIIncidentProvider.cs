using System.Xml;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Infrastructure.Features.Awareness.Providers.Incidents.Configurations;

namespace Curvia.Infrastructure.Features.Awareness.Providers.Incidents;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches live traffic incidents from DATEX II feeds.
///              Each country with a configured feed is supported; countries
///              without a feed simply return empty (no error).
///
///              Supported free feeds (as of 2026):
///                BE/Flanders : Verkeerscentrum (free with registration)
///                BE/Wallonia : SPW (free with registration)
///                NL          : NDW DATEX II (completely free, best quality)
///                FR          : Bison Futé (free)
///                DE          : BASt open data (free)
///                AT          : ASFINAG DATEX II (free)
///
///              Format support: DATEX II JSON (primary) and DATEX II XML (fallback).
///              The NDW feed uses a custom GeoJSON format handled separately.
/// </summary>
public sealed class DatexIIIncidentProvider : IIncidentFeedProvider
{
	#region Fields
	private readonly HttpClient _httpClient;
	private readonly AwarenessSyncOptions _options;
	private readonly ILogger<DatexIIIncidentProvider> _logger;

	public string ProviderName => "datex2";

	public IReadOnlyList<string> SupportedCountryCodes
		=> _options.IncidentFeeds.Select(f => f.CountryCode).Distinct().ToList();
	#endregion

	#region Constructor
	public DatexIIIncidentProvider(
		HttpClient httpClient,
		IOptions<AwarenessSyncOptions> options,
		ILogger<DatexIIIncidentProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IIncidentFeedProvider
	public async Task<IReadOnlyList<IncidentRecord>> FetchAsync(
		string countryCode, CancellationToken ct = default)
	{
		var feeds = _options.IncidentFeeds
			.Where(f => f.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (feeds.Count == 0)
		{
			_logger.LogDebug("No incident feed configured for {CountryCode}.", countryCode);
			return Array.Empty<IncidentRecord>();
		}

		var results = new List<IncidentRecord>();
		foreach (var feed in feeds)
		{
			var records = await FetchFeedAsync(feed, ct);
			results.AddRange(records);
		}

		return results;
	}
	#endregion

	#region Private helpers
	private async Task<IReadOnlyList<IncidentRecord>> FetchFeedAsync(
		IncidentFeedConfig feed, CancellationToken ct)
	{
		_logger.LogInformation("Fetching DATEX II incidents for {CountryCode} from {Url}.",
			feed.CountryCode, feed.Url);

		var request = new HttpRequestMessage(HttpMethod.Get, feed.Url);
		if (!string.IsNullOrEmpty(feed.ApiKey))
			request.Headers.Add("Authorization", $"Bearer {feed.ApiKey}");

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Incident feed request failed for {CountryCode}.", feed.CountryCode);
			return Array.Empty<IncidentRecord>();
		}

		var content = await response.Content.ReadAsStringAsync(ct);

		return feed.Format switch
		{
			"DatexII_JSON" => ParseDatexIIJson(content, feed.CountryCode),
			"DatexII_XML" => ParseDatexIIXml(content, feed.CountryCode),
			"NdwJson" => ParseNdwJson(content, feed.CountryCode),
			_ => ParseDatexIIJson(content, feed.CountryCode)
		};
	}

	// ─── DATEX II JSON ───────────────────────────────────────────

	private IReadOnlyList<IncidentRecord> ParseDatexIIJson(string json, string countryCode)
	{
		var records = new List<IncidentRecord>();
		try
		{
			using var doc = JsonDocument.Parse(json);

			// DATEX II JSON structure: {"d2LogicalModel": {"situationPublication": {"situation": [...]}}}
			if (!doc.RootElement.TryGetProperty("d2LogicalModel", out var model)) return records;
			if (!model.TryGetProperty("situationPublication", out var pub)) return records;
			if (!pub.TryGetProperty("situation", out var situations)) return records;

			foreach (var situation in situations.EnumerateArray())
			{
				if (!situation.TryGetProperty("situationRecord", out var sitRecords)) continue;

				// situation.id used as part of ExternalId
				var situationId = situation.TryGetProperty("id", out var sId)
					? sId.GetString() ?? Guid.NewGuid().ToString()
					: Guid.NewGuid().ToString();

				var recordArr = sitRecords.ValueKind == JsonValueKind.Array
					? sitRecords.EnumerateArray().ToList()
					: new List<JsonElement> { sitRecords };

				foreach (var rec in recordArr)
				{
					try
					{
						var incident = ParseDatexIISituationRecord(rec, situationId, countryCode);
						if (incident is not null) records.Add(incident);
					}
					catch { /* skip malformed records */ }
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse DATEX II JSON for {CountryCode}.", countryCode);
		}
		return records;
	}

	private static IncidentRecord? ParseDatexIISituationRecord(
		JsonElement rec, string situationId, string countryCode)
	{
		var recordId = rec.TryGetProperty("id", out var rId)
			? rId.GetString() ?? Guid.NewGuid().ToString()
			: Guid.NewGuid().ToString();

		// Coordinates from groupOfLocations > locationForDisplay
		double lat = 0, lon = 0;
		if (rec.TryGetProperty("groupOfLocations", out var locs)
			&& locs.TryGetProperty("locationForDisplay", out var displayLoc))
		{
			lat = displayLoc.TryGetProperty("latitude", out var la) ? la.GetDouble() : 0;
			lon = displayLoc.TryGetProperty("longitude", out var lo) ? lo.GetDouble() : 0;
		}

		if (lat == 0 && lon == 0) return null; // no coordinates

		var incidentType = ClassifyRecord(rec);
		var severity = ExtractSeverity(rec);
		var description = ExtractDescription(rec);

		var now = DateTime.UtcNow;
		var from = now;
		var until = now.AddHours(2); // DATEX II default validity window

		if (rec.TryGetProperty("validity", out var validity))
		{
			if (validity.TryGetProperty("validityTimeSpecification", out var timeSpec))
			{
				if (timeSpec.TryGetProperty("overallStartTime", out var start)
					&& DateTime.TryParse(start.GetString(), out var parsedStart))
					from = parsedStart.ToUniversalTime();

				if (timeSpec.TryGetProperty("overallEndTime", out var end)
					&& DateTime.TryParse(end.GetString(), out var parsedEnd))
					until = parsedEnd.ToUniversalTime();
			}
		}

		if (until <= DateTime.UtcNow) return null;

		return new IncidentRecord(
			ExternalId: $"datex2:{situationId}:{recordId}",
			Latitude: lat,
			Longitude: lon,
			IncidentType: incidentType,
			Severity: severity,
			Description: description,
			ValidFromUtc: from,
			ValidUntilUtc: until);
	}

	private static IncidentType ClassifyRecord(JsonElement rec)
	{
		// DATEX II uses xsi:type to discriminate subtypes
		var xsiType = rec.TryGetProperty("@xsi.type", out var t) ? t.GetString() ?? "" : "";
		return xsiType.ToLowerInvariant() switch
		{
			var x when x.Contains("accident") => IncidentType.Accident,
			var x when x.Contains("roadclosure")
					 || x.Contains("road_closure") => IncidentType.RoadClosure,
			var x when x.Contains("obstruction") => IncidentType.Obstruction,
			var x when x.Contains("weather")
					 || x.Contains("poorroad") => IncidentType.WeatherHazard,
			var x when x.Contains("roadwork")
					 || x.Contains("road_work") => IncidentType.WorkInProgress,
			var x when x.Contains("abnormal") => IncidentType.AbnormalLoad,
			_ => IncidentType.Other
		};
	}

	private static IncidentSeverity ExtractSeverity(JsonElement rec)
	{
		if (rec.TryGetProperty("severity", out var sev))
		{
			return sev.GetString()?.ToLowerInvariant() switch
			{
				"high" or "highest" => IncidentSeverity.High,
				"medium" or "normal" => IncidentSeverity.Medium,
				"low" or "lowest" => IncidentSeverity.Low,
				_ => IncidentSeverity.Unknown
			};
		}
		return IncidentSeverity.Unknown;
	}

	private static string? ExtractDescription(JsonElement rec)
	{
		if (rec.TryGetProperty("generalPublicComment", out var comment))
		{
			if (comment.TryGetProperty("comment", out var commentValue)
				&& commentValue.TryGetProperty("values", out var values))
			{
				foreach (var v in values.EnumerateArray())
				{
					var s = v.GetString();
					if (!string.IsNullOrWhiteSpace(s)) return s.Length > 500 ? s[..500] : s;
				}
			}
		}
		return null;
	}

	// ─── DATEX II XML ────────────────────────────────────────────

	private IReadOnlyList<IncidentRecord> ParseDatexIIXml(string xml, string countryCode)
	{
		// XML parsing: convert DATEX II XML to JSON-equivalent structure
		// then delegate to JSON parser for reuse. For MVP the JSON path is preferred.
		// This method handles feeds that only publish XML (e.g. older national systems).
		try
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			var jsonStr = System.Text.Json.JsonSerializer.Serialize(
				ConvertXmlToDict(doc.DocumentElement!));
			return ParseDatexIIJson(jsonStr, countryCode);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse DATEX II XML for {CountryCode}.", countryCode);
			return Array.Empty<IncidentRecord>();
		}
	}

	private static Dictionary<string, object?> ConvertXmlToDict(XmlElement el)
	{
		var dict = new Dictionary<string, object?>();
		foreach (XmlAttribute attr in el.Attributes)
			dict[$"@{attr.Name}"] = attr.Value;

		foreach (XmlNode child in el.ChildNodes)
		{
			if (child is XmlElement childEl)
			{
				var childDict = ConvertXmlToDict(childEl);
				if (dict.ContainsKey(child.Name))
				{
					if (dict[child.Name] is List<object> list)
						list.Add(childDict);
					else
						dict[child.Name] = new List<object> { dict[child.Name]!, childDict };
				}
				else
					dict[child.Name] = childDict;
			}
		}
		return dict;
	}

	// ─── NDW GeoJSON (Netherlands) ───────────────────────────────

	private IReadOnlyList<IncidentRecord> ParseNdwJson(string json, string countryCode)
	{
		var records = new List<IncidentRecord>();
		try
		{
			using var doc = JsonDocument.Parse(json);
			if (!doc.RootElement.TryGetProperty("features", out var features)) return records;

			foreach (var feature in features.EnumerateArray())
			{
				try
				{
					var props = feature.GetProperty("properties");
					var geom = feature.GetProperty("geometry");

					if (geom.GetProperty("type").GetString() != "Point") continue;
					var coords = geom.GetProperty("coordinates");
					var lon = coords[0].GetDouble();
					var lat = coords[1].GetDouble();

					var id = props.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
					var type = props.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "other";

					var from = DateTime.UtcNow;
					var until = DateTime.UtcNow.AddHours(1);
					if (props.TryGetProperty("startTime", out var startEl) && DateTime.TryParse(startEl.GetString(), out var parsedStart))
						from = parsedStart.ToUniversalTime();
					if (props.TryGetProperty("stopTime", out var stopEl) && DateTime.TryParse(stopEl.GetString(), out var parsedStop))
						until = parsedStop.ToUniversalTime();

					if (until <= DateTime.UtcNow) continue;

					var incidentType = type?.ToLowerInvariant() switch
					{
						"accident" => IncidentType.Accident,
						"roadworks" => IncidentType.WorkInProgress,
						"closure" => IncidentType.RoadClosure,
						_ => IncidentType.Other
					};

					records.Add(new IncidentRecord(
						ExternalId: $"ndw:{id}",
						Latitude: lat,
						Longitude: lon,
						IncidentType: incidentType,
						Severity: IncidentSeverity.Unknown,
						Description: null,
						ValidFromUtc: from,
						ValidUntilUtc: until));
				}
				catch { }
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse NDW JSON for {CountryCode}.", countryCode);
		}
		return records;
	}
	#endregion
}