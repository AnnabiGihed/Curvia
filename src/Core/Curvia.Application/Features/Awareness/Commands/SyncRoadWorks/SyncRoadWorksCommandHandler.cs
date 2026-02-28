using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncRoadWorksCommand"/>.
///              Fetches road-work records from all applicable providers in parallel
///              (resilient — one failing provider does not abort the others), upserts
///              them by natural key (ExternalId + Source + CountryCode), and purges
///              expired records after each run.
///
///              Returns Result.Failure when one or more providers fail so that Hangfire marks
///              the per-country job as failed and retries it automatically.
/// </summary>
internal sealed class SyncRoadWorksCommandHandler : ICommandHandler<SyncRoadWorksCommand>
{
	#region Dependencies
	/// <summary>All registered road-work data providers.</summary>
	private readonly IEnumerable<IRoadWorkDataProvider> _providers;

	/// <summary>Persistence contract for <see cref="RoadWork"/> aggregates.</summary>
	private readonly IRoadWorkRepository _repository;

	/// <summary>Logger for this handler.</summary>
	private readonly ILogger<SyncRoadWorksCommandHandler> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncRoadWorksCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered road-work data providers.</param>
	/// <param name="repository">Persistence contract for road-work aggregates.</param>
	/// <param name="logger">Logger instance.</param>
	public SyncRoadWorksCommandHandler(IEnumerable<IRoadWorkDataProvider> providers, IRoadWorkRepository repository, ILogger<SyncRoadWorksCommandHandler> logger)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the sync pipeline: filter providers, fetch in parallel (resilient), upsert, then delete expired records.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A success result when all providers succeed and the sync completes.
	/// A failure result when one or more providers fail — Hangfire will mark the job as failed.
	/// </returns>
	public async Task<Result> Handle(SyncRoadWorksCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();
		_logger.LogInformation("Road-work sync started for country {Country}.", country);

		// Only invoke providers that support this country.
		var applicable = _providers
			.Where(p => p.SupportedCountryCodes.Count == 0 || p.SupportedCountryCodes.Contains(country))
			.ToList();

		if (applicable.Count == 0)
		{
			_logger.LogDebug("Road-work sync for {Country}: no applicable providers.", country);
			return Result.Success();
		}

		// Resilient parallel fetch — one provider failing does not abort the others.
		var (records, hadProviderFailure, failedProviders) = await FetchFromApplicableProvidersAsync(applicable, country, ct);

		_logger.LogInformation("Road-work sync for {Country}: {Count} records fetched across {Providers} provider(s).", country, records.Count, applicable.Count);

		// Upsert strategy: find by externalId + source + country; update or create.
		var inserted = 0;
		var updated = 0;
		var skipped = 0;

		foreach (var record in records)
		{
			var source = DeriveSource(record.ExternalId, applicable);
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, source, country, ct);

			if (existing is null)
			{
				var rwResult = RoadWork.Create(record.ExternalId, source, country, record.Latitude, record.Longitude, record.Title, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				if (rwResult.IsFailure)
				{
					_logger.LogWarning("Road-work sync for {Country}: failed to create road work {ExternalId} — {Error}.", country, record.ExternalId, rwResult.Error);
					skipped++;
					continue;
				}
				await _repository.AddAsync(rwResult.Value, ct);
				inserted++;
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude, record.Title, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.UpdateAsync(existing, ct);
				updated++;
			}
		}

		// Purge expired records after each sync pass.
		await _repository.DeleteExpiredAsync(ct);

		_logger.LogInformation("Road-work sync for {Country} complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped.", country, inserted, updated, skipped);

		// Propagate provider failures so Hangfire marks the per-country job as failed.
		if (hadProviderFailure)
			return Result.Failure(new Error("ProviderError", $"Road-work sync for {country} had failing provider(s): {string.Join(", ", failedProviders)}."));

		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches road-work records from applicable providers in parallel.
	/// Each provider is wrapped in an individual try/catch so that one failing provider
	/// does not cancel results from the remaining providers.
	/// </summary>
	/// <param name="providers">Providers that support the target country.</param>
	/// <param name="country">ISO 3166-1 alpha-2 country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A tuple of: the combined list of raw road-work records from providers that succeeded,
	/// a flag indicating whether any provider failed, and the list of failed provider names.
	/// </returns>
	private async Task<(List<RoadWorkRecord> Records, bool HadFailure, List<string> FailedProviders)>
		FetchFromApplicableProvidersAsync(List<IRoadWorkDataProvider> providers, string country, CancellationToken ct)
	{
		var failedProviders = new List<string>();

		var tasks = providers.Select(async provider =>
		{
			try
			{
				var records = await provider.FetchAsync(country, ct);
				return (Records: records, Failed: false, ProviderName: provider.ProviderName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Road-work provider {Provider} failed for country {Country}.", provider.ProviderName, country);
				return (Records: (IReadOnlyList<RoadWorkRecord>)Array.Empty<RoadWorkRecord>(), Failed: true, ProviderName: provider.ProviderName);
			}
		}).ToList();

		var results = await Task.WhenAll(tasks);

		foreach (var result in results.Where(r => r.Failed))
			failedProviders.Add(result.ProviderName);

		var allRecords = results.SelectMany(r => r.Records).ToList();
		return (allRecords, failedProviders.Count > 0, failedProviders);
	}

	/// <summary>
	/// Derives the source name from the external ID prefix, falling back to the first provider's name.
	/// </summary>
	/// <param name="externalId">The external identifier of the road-work record.</param>
	/// <param name="providers">The list of applicable providers for the current sync run.</param>
	/// <returns>A short source identifier string such as "osm", "gipod", or "unknown".</returns>
	private static string DeriveSource(string externalId, List<IRoadWorkDataProvider> providers)
	{
		if (externalId.StartsWith("osm:", StringComparison.OrdinalIgnoreCase))
			return "osm";
		if (externalId.StartsWith("gipod:", StringComparison.OrdinalIgnoreCase))
			return "gipod";
		return providers.FirstOrDefault()?.ProviderName ?? "unknown";
	}
	#endregion
}