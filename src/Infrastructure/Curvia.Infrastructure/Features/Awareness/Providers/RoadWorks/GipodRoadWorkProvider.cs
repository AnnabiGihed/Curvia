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
	/// <summary>Base URL for the GIPOD manifestations endpoint.</summary>
	private const string BaseUrl = "https://api.gipod.be/api/v1/manifestations";

	/// <summary>Number of records to request per page.</summary>
	private const int PageSize = 500;
	#endregion

	#region Fields
	/// <summary>HttpClient configured for the GIPOD API.</summary>
	private readonly HttpClient _httpClient;

	/// <summary>Logger for fetch lifecycle and error events.</summary>
	private readonly ILogger<GipodRoadWorkProvider> _logger;

	/// <summary>Short provider identifier used as the Source on persisted aggregates.</summary>
	public string ProviderName => "gipod";

	/// <summary>Only Belgium is covered by GIPOD.</summary>
	public IReadOnlyList<string> SupportedCountryCodes => new[] { "BE" };
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="GipodRoadWorkProvider"/>.
	/// </summary>
	/// <param name="httpClient">HttpClient configured for the GIPOD API.</param>
	/// <param name="logger">Logger instance.</param>
	public GipodRoadWorkProvider(HttpClient httpClient, ILogger<GipodRoadWorkProvider> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region IRoadWorkDataProvider
	/// <summary>
	/// Fetches all active GIPOD manifestations for Belgium using offset pagination.
	/// Throws on any HTTP or network failure so the caller's resilience layer can mark this provider as failed.
	/// </summary>
	/// <param name="countryCode">ISO country code. Returns empty for non-BE codes.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>All active road-work records from GIPOD.</returns>
	public async Task<IReadOnlyList<RoadWorkRecord>> FetchAsync(string countryCode, CancellationToken ct = default)
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
			if (page is null || page.Count == 0)
				break;

			result.AddRange(page);
			if (page.Count < PageSize)
				break;
			offset += PageSize;
		}

		_logger.LogInformation("GIPOD: fetched {Count} road works.", result.Count);
		return result;
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches a single page from the GIPOD API.
	/// Logs the error and re-throws on any network or HTTP failure — callers rely on the exception
	/// propagating so that the sync handler can mark this provider as failed.
	/// </summary>
	/// <param name="url">Full paginated URL including limit and offset.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Parsed road-work records for this page, or null if the page is empty.</returns>
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
			throw;
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return ParseGipodResponse(json);
	}

	/// <summary>
	/// Parses a GeoJSON FeatureCollection returned by the GIPOD API into road-work records.
	/// </summary>
	/// <param name="json">Raw JSON string from the GIPOD response body.</param>
	/// <returns>Parsed road-work records. Empty list when no valid features are found.</returns>
	private static List<RoadWorkRecord> ParseGipodResponse(string json)
	{
		var records = new List<RoadWorkRecord>();

		using var doc = JsonDocument.Parse(json);

		if (!doc.RootElement.TryGetProperty("features", out var features))
			return records;

		foreach (var feature in features.EnumerateArray())
		{
			try
			{
				var props = feature.GetProperty("properties");
				var geom = feature.GetProperty("geometry");

				if (!TryExtractCentroid(geom, out var lat, out var lon))
					continue;

				var id = props.TryGetProperty("id", out var idEl)
					? idEl.GetRawText().Trim('"')
					: Guid.NewGuid().ToString();

				var title = props.TryGetProperty("description", out var descEl)
					? descEl.GetString() ?? "Road works"
					: "Road works";

				var validFrom = DateTime.UtcNow;
				var validUntil = DateTime.UtcNow.AddDays(30);

				if (props.TryGetProperty("startDateTime", out var startEl) && DateTime.TryParse(startEl.GetString(), out var parsedStart))
					validFrom = parsedStart.ToUniversalTime();

				if (props.TryGetProperty("endDateTime", out var endEl) && DateTime.TryParse(endEl.GetString(), out var parsedEnd))
					validUntil = parsedEnd.ToUniversalTime();

				if (validUntil <= DateTime.UtcNow)
					continue;

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
				// Skip malformed individual features — do not abort the entire page.
			}
		}

		return records;
	}

	/// <summary>
	/// Attempts to extract a representative centroid from a GeoJSON geometry (Point, Polygon, or MultiPolygon).
	/// </summary>
	/// <param name="geom">The GeoJSON geometry element.</param>
	/// <param name="lat">Extracted latitude, or 0 on failure.</param>
	/// <param name="lon">Extracted longitude, or 0 on failure.</param>
	/// <returns>True when a centroid was successfully extracted; otherwise false.</returns>
	private static bool TryExtractCentroid(JsonElement geom, out double lat, out double lon)
	{
		lat = 0;
		lon = 0;

		if (!geom.TryGetProperty("type", out var typeEl) || !geom.TryGetProperty("coordinates", out var coords))
			return false;

		var type = typeEl.GetString();

		if (type == "Point")
		{
			lon = coords[0].GetDouble();
			lat = coords[1].GetDouble();
			return true;
		}

		if (type == "Polygon" && coords.GetArrayLength() > 0)
		{
			var ring = coords[0];
			return TryAverageCentroid(ring, out lat, out lon);
		}

		if (type == "MultiPolygon" && coords.GetArrayLength() > 0)
		{
			var ring = coords[0][0];
			return TryAverageCentroid(ring, out lat, out lon);
		}

		return false;
	}

	/// <summary>
	/// Computes the average of all coordinate pairs in a ring as an approximate centroid.
	/// </summary>
	/// <param name="ring">Array of [lon, lat] coordinate pairs.</param>
	/// <param name="lat">Average latitude.</param>
	/// <param name="lon">Average longitude.</param>
	/// <returns>True when the ring contained at least one coordinate pair.</returns>
	private static bool TryAverageCentroid(JsonElement ring, out double lat, out double lon)
	{
		lat = 0;
		lon = 0;
		var count = 0;

		foreach (var point in ring.EnumerateArray())
		{
			lon += point[0].GetDouble();
			lat += point[1].GetDouble();
			count++;
		}

		if (count == 0)
			return false;

		lat /= count;
		lon /= count;
		return true;
	}
	#endregion
}