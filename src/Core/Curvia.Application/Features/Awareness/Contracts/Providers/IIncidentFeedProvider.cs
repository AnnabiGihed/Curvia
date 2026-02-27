using Curvia.Application.Features.Awareness.Contracts.Records;

namespace Curvia.Application.Features.Awareness.Contracts.Providers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Port for live incident feed providers (DATEX II and equivalents).
///              One implementation per feed format. Country-to-feed mapping is
///              driven by AwarenessFeedOptions configuration.
/// </summary>
public interface IIncidentFeedProvider
{
	string ProviderName { get; }
	IReadOnlyList<string> SupportedCountryCodes { get; }
	Task<IReadOnlyList<IncidentRecord>> FetchAsync(string countryCode, CancellationToken ct = default);
}