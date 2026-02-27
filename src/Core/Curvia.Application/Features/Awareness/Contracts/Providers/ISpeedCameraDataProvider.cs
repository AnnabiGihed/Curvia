using Curvia.Application.Features.Awareness.Contracts.Records;

namespace Curvia.Application.Features.Awareness.Contracts.Providers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for speed camera data providers.
///              Implementations: OsmSpeedCameraProvider, OpenSpeedCamProvider.
///              The sync service calls all registered providers and merges + deduplicates.
/// </summary>
public interface ISpeedCameraDataProvider
{
	string ProviderName { get; }

	/// <summary>
	/// Fetches speed cameras for a specific country bounding box.
	/// </summary>
	/// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
	/// <param name="ct">Cancellation token.</param>
	Task<IReadOnlyList<SpeedCameraRecord>> FetchAsync(string countryCode, CancellationToken ct = default);
}