using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Awareness.Enums;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncHazards;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncHazardsCommand"/>.
///              Fetches permanent road hazard records (potholes, dangerous crossings, landslide
///              zones, etc.) from all registered providers in parallel (resilient — one failing
///              provider does not abort the others), deduplicates hazards within 30 m of each
///              other, deletes existing data per source for the country, then upserts the
///              deduplicated dataset.
///              Runs monthly per country — permanent hazard locations rarely change.
///
///              Returns Result.Failure when one or more providers fail so that Hangfire marks
///              the per-country job as failed and retries it automatically.
/// </summary>
internal sealed class SyncHazardsCommandHandler : ICommandHandler<SyncHazardsCommand>
{
	#region Constants
	/// <summary>Maximum distance in metres between two hazards from different sources to be considered the same physical hazard.</summary>
	private const double DeduplicationDistanceMeters = 30.0;

	/// <summary>Earth's approximate radius in metres, used in the Haversine distance calculation.</summary>
	private const double EarthRadiusMeters = 6_371_000.0;
	#endregion

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
	/// Executes the hazard sync pipeline: fetch in parallel (resilient), deduplicate, delete existing per source, then upsert.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A success result when all providers succeed and the sync completes.
	/// A failure result when one or more providers fail — Hangfire will mark the job as failed.
	/// </returns>
	public async Task<Result> Handle(SyncHazardsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();
		_logger.LogInformation("Hazard sync started for country {Country}.", country);

		var (allRecords, hadProviderFailure, failedProviders) = await FetchFromAllProvidersAsync(country, ct);

		if (allRecords.Count == 0 && !hadProviderFailure)
		{
			_logger.LogWarning("Hazard sync for {Country}: no records returned from any provider.", country);
			return Result.Success();
		}

		var deduplicated = DeduplicateHazards(allRecords);
		_logger.LogInformation("Hazard sync for {Country}: {Total} raw records → {Deduped} after deduplication.", country, allRecords.Count, deduplicated.Count);

		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		var inserted = 0;
		var updated = 0;
		var skipped = 0;

		foreach (var record in deduplicated)
		{
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, record.Source, country, ct);

			if (existing is null)
			{
				var hazardResult = Hazard.Create(record.ExternalId, record.Source, country, record.Latitude, record.Longitude, record.HazardType, record.Description);
				if (hazardResult.IsFailure)
				{
					_logger.LogWarning("Hazard sync for {Country}: failed to create hazard {ExternalId} — {Error}.", country, record.ExternalId, hazardResult.Error);
					skipped++;
					continue;
				}
				await _repository.AddAsync(hazardResult.Value, ct);
				inserted++;
			}
			else
			{
				var syncResult = existing.Sync(record.Latitude, record.Longitude, record.HazardType, record.Description);
				if (syncResult.IsFailure)
				{
					_logger.LogWarning("Hazard sync for {Country}: failed to sync hazard {ExternalId} — {Error}.", country, record.ExternalId, syncResult.Error);
					skipped++;
					continue;
				}
				await _repository.UpdateAsync(existing, ct);
				updated++;
			}
		}

		_logger.LogInformation("Hazard sync for {Country} complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped.", country, inserted, updated, skipped);

		// Propagate provider failures so Hangfire marks the per-country job as failed.
		if (hadProviderFailure)
			return Result.Failure(new Error("ProviderError", $"Hazard sync for {country} had failing provider(s): {string.Join(", ", failedProviders)}."));

		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches hazard records from all providers in parallel.
	/// Individual provider failures are caught and logged — they do not abort the sync.
	/// </summary>
	/// <param name="country">Uppercase ISO country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A tuple of: the flat list of all records from successful providers,
	/// a flag indicating whether any provider failed, and the list of failed provider names.
	/// </returns>
	private async Task<(List<HazardRecord> Records, bool HadFailure, List<string> FailedProviders)>
		FetchFromAllProvidersAsync(string country, CancellationToken ct)
	{
		var failedProviders = new List<string>();

		var tasks = _providers.Select(async p =>
		{
			try
			{
				var records = await p.FetchAsync(country, ct);
				return (Records: records, Failed: false, ProviderName: p.ProviderName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Hazard provider {Provider} failed for {Country}.", p.ProviderName, country);
				return (Records: (IReadOnlyList<HazardRecord>)Array.Empty<HazardRecord>(), Failed: true, ProviderName: p.ProviderName);
			}
		});

		var results = await Task.WhenAll(tasks);

		foreach (var result in results.Where(r => r.Failed))
			failedProviders.Add(result.ProviderName);

		var allRecords = results.SelectMany(r => r.Records).ToList();
		return (allRecords, failedProviders.Count > 0, failedProviders);
	}

	/// <summary>
	/// Removes duplicate hazards (same physical location, different sources) using Haversine distance.
	/// When two hazards from different sources are within <see cref="DeduplicationDistanceMeters"/>,
	/// the one that appears first in the list is kept.
	/// </summary>
	/// <param name="records">Flat list of all records from all providers.</param>
	/// <returns>Deduplicated list.</returns>
	private static List<HazardRecord> DeduplicateHazards(List<HazardRecord> records)
	{
		var kept = new List<HazardRecord>();

		foreach (var record in records)
		{
			var isDuplicate = kept.Any(k =>
				k.Source != record.Source &&
				Haversine(k.Latitude, k.Longitude, record.Latitude, record.Longitude) <= DeduplicationDistanceMeters);

			if (!isDuplicate)
				kept.Add(record);
		}

		return kept;
	}

	/// <summary>
	/// Computes the Haversine distance in metres between two WGS84 coordinates.
	/// </summary>
	/// <param name="lat1">Latitude of the first point.</param>
	/// <param name="lon1">Longitude of the first point.</param>
	/// <param name="lat2">Latitude of the second point.</param>
	/// <param name="lon2">Longitude of the second point.</param>
	/// <returns>Distance in metres.</returns>
	private static double Haversine(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = (lat2 - lat1) * Math.PI / 180.0;
		var dLon = (lon2 - lon1) * Math.PI / 180.0;
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
	}
	#endregion
}