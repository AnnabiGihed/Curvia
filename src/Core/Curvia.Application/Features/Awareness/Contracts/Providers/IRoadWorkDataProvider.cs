using Curvia.Application.Features.Awareness.Contracts.Records;

namespace Curvia.Application.Features.Awareness.Contracts.Providers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for road work data providers.
///              One implementation per country (e.g. GipodRoadWorkProvider for Belgium,
///              OsmRoadWorkProvider as the fallback for countries without dedicated feeds).
///              The factory resolves the right provider(s) per country code.
/// </summary>
public interface IRoadWorkDataProvider
{
	string ProviderName { get; }

	/// <summary>
	/// The country codes this provider supports. Empty = supports all countries.
	/// </summary>
	IReadOnlyList<string> SupportedCountryCodes { get; }

	Task<IReadOnlyList<RoadWorkRecord>> FetchAsync(string countryCode, CancellationToken ct = default);
}