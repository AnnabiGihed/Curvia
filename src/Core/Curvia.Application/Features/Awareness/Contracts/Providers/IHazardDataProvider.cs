using Curvia.Application.Features.Awareness.Contracts.Records;

namespace Curvia.Application.Features.Awareness.Contracts.Providers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for hazard data providers.
///              Implementation: OsmHazardProvider.
/// </summary>
public interface IHazardDataProvider
{
	string ProviderName { get; }
	Task<IReadOnlyList<HazardRecord>> FetchAsync(string countryCode, CancellationToken ct = default);
}