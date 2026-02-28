using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Curvia.Infrastructure.Features.Awareness.Providers.RoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches active road works from the GIPOD OGC API Features service.
///
///              GIPOD (Generiek Informatieplatform Openbaar Domein) is managed by Athumi
///              and is the legally mandated registration platform for all public domain
///              occupations in Belgium. Coverage is authoritative for Flanders and Brussels;
///              Wallonia coverage is improving.
///
///              API : https://geo.api.vlaanderen.be/GIPOD/ogc/features
///              Auth: none — open public endpoint, no registration required.
///              Format: OGC API Features / GeoJSON FeatureCollection.
///              Scope: all occupancies that are concretely planned or ongoing within the
///              next 6 months (server-side temporal filter — no client-side expiry needed).
///
///              Collection used: INNAME_PUNT (point geometries) — avoids centroid
///              extraction from polygon/multipolygon that the retired legacy API required.
///
///              Pagination: limit + startIndex query parameters (OGC standard).
///
///              ExternalId format: "gipod:{gipodId}"
///
///              Migration note:
///              The old domain api.gipod.be never existed.
///              The domain api.gipod.vlaanderen.be was the correct legacy address but was
///              permanently shut down on 30 July 2024 when Athumi took over GIPOD.
/// </summary>
public sealed class GipodRoadWorkProvider : IRoadWorkDataProvider
{
	#region Constants
	/// <summary>Base URL for the GIPOD OGC API Features service.</summary>
	private const string BaseUrl = "https://geo.api.vlaanderen.be/GIPOD/ogc/features/v1/collections/INNAME_PUNT/items";

	/// <summary>Number of features to request per page. GIPOD allows up to 1000.</summary>
	private const int PageSize = 1000;
	#endregion

	#region Properties
	/// <summary>Short provider identifier used as the Source on persisted aggregates.</summary>
	public string ProviderName => "gipod";

	/// <summary>Only Belgium is covered by GIPOD.</summary>
	public IReadOnlyList<string> SupportedCountryCodes => new[] { "BE" };
	#endregion

	#region Fields
	/// <summary>HttpClient configured for the GIPOD OGC API.</summary>
	private readonly HttpClient _httpClient;

	/// <summary>Logger for fetch lifecycle events.</summary>
	private readonly ILogger<GipodRoadWorkProvider> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="GipodRoadWorkProvider"/>.
	/// </summary>
	/// <param name="httpClient">HttpClient configured for the GIPOD OGC API.</param>
	/// <param name="logger">Logger instance.</param>
	public GipodRoadWorkProvider(HttpClient httpClient, ILogger<GipodRoadWorkProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IRoadWorkDataProvider
	/// <summary>
	/// Fetches all active GIPOD occupancies for Belgium using OGC API Features pagination.
	/// Re-throws on any network or HTTP failure — error logging is owned by the
	/// caller's resilience layer which has full provider + country context.
	/// </summary>
	/// <param name="countryCode">ISO country code. Returns empty for non-BE codes.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>All active road-work records from GIPOD.</returns>
	public async Task<IReadOnlyList<RoadWorkRecord>> FetchAsync(string countryCode, CancellationToken ct = default)
	{
		if (!countryCode.Equals("BE", StringComparison.OrdinalIgnoreCase))
			return Array.Empty<RoadWorkRecord>();

		_logger.LogInformation("Fetching GIPOD road works via OGC API Features.");

		var result = new List<RoadWorkRecord>();
		var startIndex = 0;

		while (true)
		{
			var url = $"{BaseUrl}?f=application%2Fgeo%2Bjson&limit={PageSize}&startIndex={startIndex}";
			var page = await FetchPageAsync(url, ct);

			if (page.Count == 0)
				break;

			result.AddRange(page);

			if (page.Count < PageSize)
				break;

			startIndex += PageSize;
		}

		_logger.LogInformation("GIPOD: fetched {Count} road works.", result.Count);
		return result;
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches and parses a single page from the GIPOD OGC API Features endpoint.
	/// Re-throws on any network or HTTP failure without logging — the calling handler
	/// catches the exception and logs at Error level with full provider and country context.
	/// </summary>
	/// <param name="url">Full paginated URL including limit and startIndex.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Parsed road-work records for this page.</returns>
	private async Task<List<RoadWorkRecord>> FetchPageAsync(string url, CancellationToken ct)
	{
		var response = await _httpClient.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseFeatureCollection(json);
	}

	/// <summary>
	/// Parses a GeoJSON FeatureCollection returned by the GIPOD OGC API Features service.
	/// The INNAME_PUNT collection returns Point geometries — no centroid extraction required.
	/// </summary>
	/// <param name="json">Raw GeoJSON string from the response body.</param>
	/// <returns>Parsed road-work records. Empty list when no valid features are found.</returns>
	private static List<RoadWorkRecord> ParseFeatureCollection(string json)
	{
		var records = new List<RoadWorkRecord>();

		using var doc = JsonDocument.Parse(json);

		if (!doc.RootElement.TryGetProperty("features", out var features))
			return records;

		foreach (var feature in features.EnumerateArray())
		{
			try
			{
				if (!feature.TryGetProperty("geometry", out var geometry))
					continue;
				if (!feature.TryGetProperty("properties", out var props))
					continue;

				// INNAME_PUNT uses Point geometry — coordinates are [lon, lat].
				if (!geometry.TryGetProperty("coordinates", out var coords))
					continue;

				var lon = coords[0].GetDouble();
				var lat = coords[1].GetDouble();

				// gipodId is the stable natural key — fall back to feature id if absent.
				string externalId;
				if (props.TryGetProperty("gipodId", out var gipodIdEl))
					externalId = $"gipod:{gipodIdEl.GetRawText().Trim('"')}";
				else if (feature.TryGetProperty("id", out var featureId))
					externalId = $"gipod:{featureId.GetRawText().Trim('"')}";
				else
					continue;

				// Description — GIPOD uses "omschrijving" (Dutch for "description").
				var title = "Road works";
				if (props.TryGetProperty("omschrijving", out var omschrijving) && omschrijving.ValueKind != JsonValueKind.Null)
				{
					var raw = omschrijving.GetString();
					if (!string.IsNullOrWhiteSpace(raw))
						title = raw.Length > 200 ? raw[..200] : raw;
				}

				// Temporal bounds — startDatum / eindDatum in ISO 8601.
				var validFrom = DateTime.UtcNow;
				var validUntil = DateTime.UtcNow.AddDays(30);

				if (props.TryGetProperty("startDatum", out var startEl) && startEl.ValueKind != JsonValueKind.Null
					&& DateTime.TryParse(startEl.GetString(), out var parsedStart))
					validFrom = parsedStart.ToUniversalTime();

				if (props.TryGetProperty("eindDatum", out var endEl) && endEl.ValueKind != JsonValueKind.Null
					&& DateTime.TryParse(endEl.GetString(), out var parsedEnd))
					validUntil = parsedEnd.ToUniversalTime();

				records.Add(new RoadWorkRecord(
					ExternalId: externalId,
					Latitude: lat,
					Longitude: lon,
					Title: title,
					Description: null,
					ValidFromUtc: validFrom,
					ValidUntilUtc: validUntil));
			}
			catch
			{
				// Skip malformed individual features — do not abort the entire page.
			}
		}

		return records;
	}
	#endregion
}