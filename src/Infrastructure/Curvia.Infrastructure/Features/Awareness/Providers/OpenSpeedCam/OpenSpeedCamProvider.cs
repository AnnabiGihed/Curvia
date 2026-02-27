using Curvia.Application.Features.Awareness.Contracts.Providers;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Infrastructure.Features.Awareness.Providers.Osm;
using Microsoft.Extensions.Logging;

namespace Curvia.Infrastructure.Features.Awareness.Providers.OpenSpeedCam;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Fetches European speed cameras from the OpenSpeedCam dataset.
///              OpenSpeedCam is a free, crowdsourced pan-European speed camera database
///              distributed as a downloadable CSV file — no API key required.
///
///              The full CSV is downloaded once and filtered per country by the bounding
///              box of each requested country code. This avoids hitting a remote endpoint
///              once per country — the file is cached in memory for the duration of the
///              sync run.
///
///              CSV format (comma-separated):
///                latitude,longitude,speed,direction,type,country,...
///
///              ExternalId format: "openspeedcam:{rowIndex}" — the file has no stable IDs,
///              so we use a deterministic hash of (lat, lon, direction) as a stable key.
///
///              Download URL: https://openspeedcam.org/speedcameras.csv
/// </summary>
public sealed class OpenSpeedCamProvider : ISpeedCameraDataProvider
{
	#region Constants
	private const string DownloadUrl = "https://openspeedcam.org/speedcameras.csv";
	#endregion

	#region Fields
	private readonly HttpClient _httpClient;
	private readonly ILogger<OpenSpeedCamProvider> _logger;

	// In-memory cache valid for the duration of a single sync run
	private List<OpenSpeedCamRow>? _cachedRows;

	public string ProviderName => "openspeedcam";
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
			_logger.LogWarning("OpenSpeedCam sync: no bounding box for {CountryCode}.", countryCode);
			return Array.Empty<SpeedCameraRecord>();
		}

		await EnsureCsvLoadedAsync(ct);

		if (_cachedRows is null || _cachedRows.Count == 0)
			return Array.Empty<SpeedCameraRecord>();

		// Filter rows within the country bounding box
		return _cachedRows
			.Where(r => r.Latitude >= bbox.South && r.Latitude <= bbox.North
					 && r.Longitude >= bbox.West && r.Longitude <= bbox.East)
			.Select(r => new SpeedCameraRecord(
				ExternalId: $"openspeedcam:{StableId(r)}",
				Latitude: r.Latitude,
				Longitude: r.Longitude,
				SpeedLimitKmh: r.SpeedKmh,
				Source: ProviderName,
				Direction: r.Direction))
			.ToList();
	}
	#endregion

	#region Private helpers
	private async Task EnsureCsvLoadedAsync(CancellationToken ct)
	{
		if (_cachedRows is not null) return;

		_logger.LogInformation("Downloading OpenSpeedCam CSV from {Url}.", DownloadUrl);
		try
		{
			var csv = await _httpClient.GetStringAsync(DownloadUrl, ct);
			_cachedRows = ParseCsv(csv);
			_logger.LogInformation("OpenSpeedCam CSV loaded: {Count} entries.", _cachedRows.Count);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to download OpenSpeedCam CSV.");
			_cachedRows = new List<OpenSpeedCamRow>();
		}
	}

	private static List<OpenSpeedCamRow> ParseCsv(string csv)
	{
		var rows = new List<OpenSpeedCamRow>();
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		// Skip header row
		for (var i = 1; i < lines.Length; i++)
		{
			var cols = lines[i].Split(',');
			if (cols.Length < 4) continue;

			if (!double.TryParse(cols[0].Trim(), System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out var lat)) continue;
			if (!double.TryParse(cols[1].Trim(), System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out var lon)) continue;

			int? speed = null;
			if (cols.Length > 2 && int.TryParse(cols[2].Trim(), out var s) && s > 0)
				speed = s;

			var direction = cols.Length > 3
				? ParseDirection(cols[3].Trim())
				: CameraDirection.Unknown;

			rows.Add(new OpenSpeedCamRow(lat, lon, speed, direction));
		}

		return rows;
	}

	private static CameraDirection ParseDirection(string value)
		=> value.ToLowerInvariant() switch
		{
			"both" or "b" => CameraDirection.Both,
			"forward" or "f" => CameraDirection.Forward,
			"backward" or "r" => CameraDirection.Backward,
			_ => CameraDirection.Unknown
		};

	/// <summary>
	/// Generates a stable, deterministic identifier for a camera row.
	/// Uses lat+lon+direction as the key since OpenSpeedCam has no row IDs.
	/// Truncated to 16 hex chars — collision probability is negligible at dataset scale.
	/// </summary>
	private static string StableId(OpenSpeedCamRow row)
	{
		var hash = HashCode.Combine(
			Math.Round(row.Latitude, 5),
			Math.Round(row.Longitude, 5),
			(int)row.Direction);
		return Math.Abs(hash).ToString("x");
	}

	private sealed record OpenSpeedCamRow(
		double Latitude,
		double Longitude,
		int? SpeedKmh,
		CameraDirection Direction);
	#endregion
}