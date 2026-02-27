using Microsoft.Extensions.Logging;
using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncHazards;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncHazardsCommand"/>.
///              Fetches permanent road-hazard records from all providers in parallel
///              (resilient — one failing provider does not abort the others), deletes
///              existing data per source, then bulk-inserts the fresh dataset.
///              Hazards use a delete-then-reload strategy rather than per-record upsert
///              because the data changes rarely and correctness of the full set matters more
///              than minimising write operations.
/// </summary>
internal sealed class SyncHazardsCommandHandler : ICommandHandler<SyncHazardsCommand>
{
	#region Dependencies
	/// <summary>All registered hazard data providers.</summary>
	private readonly IEnumerable<IHazardDataProvider> _providers;

	/// <summary>Persistence contract for <see cref="Hazard"/> aggregates.</summary>
	private readonly IHazardRepository _repository;

	/// <summary>Logger for this handler.</summary>
	private readonly ILogger<SyncHazardsCommandHandler> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncHazardsCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered hazard data providers.</param>
	/// <param name="repository">Persistence contract for hazard aggregates.</param>
	/// <param name="logger">Logger instance.</param>
	public SyncHazardsCommandHandler(IEnumerable<IHazardDataProvider> providers, IHazardRepository repository, ILogger<SyncHazardsCommandHandler> logger)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the hazard sync pipeline: fetch in parallel (resilient), delete existing per source, then bulk insert.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A success result when the sync completes without fatal error.</returns>
	public async Task<Result> Handle(SyncHazardsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();
		_logger.LogInformation("Hazard sync started for country {Country}.", country);

		// Resilient parallel fetch — one provider failing does not abort the others.
		var records = await FetchFromAllProvidersAsync(country, ct);

		if (records.Count == 0)
		{
			_logger.LogWarning("Hazard sync for {Country}: no records returned from any provider.", country);
			return Result.Success();
		}

		_logger.LogInformation("Hazard sync for {Country}: {Count} records fetched. Deleting existing data per source.", country, records.Count);

		// Delete existing data per source before reload.
		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		// Bulk insert fresh dataset.
		var inserted = 0;
		var skipped = 0;

		foreach (var record in records)
		{
			var hazardResult = Hazard.Create(record.ExternalId, DeriveSource(record.ExternalId), country, record.Latitude, record.Longitude, record.HazardType, record.Description);
			if (hazardResult.IsFailure)
			{
				_logger.LogWarning("Hazard sync for {Country}: failed to create hazard {ExternalId} — {Error}.", country, record.ExternalId, hazardResult.Error);
				skipped++;
				continue;
			}
			await _repository.AddAsync(hazardResult.Value, ct);
			inserted++;
		}

		_logger.LogInformation("Hazard sync for {Country} complete: {Inserted} inserted, {Skipped} skipped.", country, inserted, skipped);
		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches hazard records from all providers in parallel.
	/// Each provider is wrapped in an individual try/catch so that one failing provider
	/// does not cancel results from the remaining providers.
	/// </summary>
	/// <param name="country">ISO 3166-1 alpha-2 country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Combined list of raw hazard records from all providers that succeeded.</returns>
	private async Task<List<HazardRecord>> FetchFromAllProvidersAsync(string country, CancellationToken ct)
	{
		var tasks = _providers.Select(async provider =>
		{
			try
			{
				return await provider.FetchAsync(country, ct);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Hazard provider {Provider} failed for country {Country}.", provider.ProviderName, country);
				return Enumerable.Empty<HazardRecord>();
			}
		}).ToList();

		var results = await Task.WhenAll(tasks);
		return results.SelectMany(r => r).ToList();
	}

	/// <summary>
	/// Derives the source name from the external ID prefix.
	/// </summary>
	/// <param name="externalId">The external identifier of the hazard record.</param>
	/// <returns>"osm" for OSM-prefixed IDs, "unknown" for all others.</returns>
	private static string DeriveSource(string externalId)
	{
		if (externalId.StartsWith("osm:", StringComparison.OrdinalIgnoreCase))
			return "osm";
		return "unknown";
	}
	#endregion
}