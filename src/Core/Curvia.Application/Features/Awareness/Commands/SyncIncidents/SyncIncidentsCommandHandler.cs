using Microsoft.Extensions.Logging;
using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Records;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncIncidents;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncIncidentsCommand"/>.
///              Fetches live traffic incident records from all applicable DATEX II providers
///              in parallel (resilient — one failing provider does not abort the others),
///              upserts them by natural key (ExternalId + Source + CountryCode), and purges
///              expired incidents after each run.
///              Runs every 3 minutes per country — optimised for low-latency incident updates.
///
///              A single SaveChangesAsync is called at the end of the pipeline so that
///              the entire sync for a country (inserts + updates + expiry deletes) is
///              committed atomically in one round-trip.
///
///              Returns Result.Failure when one or more providers fail so that Hangfire marks
///              the per-country job as failed and retries it automatically.
/// </summary>
internal sealed class SyncIncidentsCommandHandler : ICommandHandler<SyncIncidentsCommand>
{
	#region Dependencies
	/// <summary>All registered incident feed providers.</summary>
	private readonly IEnumerable<IIncidentFeedProvider> _providers;

	/// <summary>Persistence contract for <see cref="Incident"/> aggregates.</summary>
	private readonly IIncidentRepository _repository;

	/// <summary>Commits all pending changes atomically: auditing + outbox + SaveChanges.</summary>
	private readonly IUnitOfWork _unitOfWork;

	/// <summary>Logger for this handler.</summary>
	private readonly ILogger<SyncIncidentsCommandHandler> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncIncidentsCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered incident feed providers.</param>
	/// <param name="repository">Persistence contract for incident aggregates.</param>
	/// <param name="unitOfWork">Unit of work used to commit the sync atomically.</param>
	/// <param name="logger">Logger instance.</param>
	public SyncIncidentsCommandHandler(IEnumerable<IIncidentFeedProvider> providers, IIncidentRepository repository, IUnitOfWork unitOfWork, ILogger<SyncIncidentsCommandHandler> logger)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the incident sync pipeline: filter providers, fetch in parallel (resilient), upsert, then delete expired records.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// A success result when all providers succeed and the sync completes.
	/// A failure result when one or more providers fail — Hangfire will mark the job as failed.
	/// Also returns success when no feed is configured for this country (not a failure condition).
	/// </returns>
	public async Task<Result> Handle(SyncIncidentsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		var applicable = _providers
			.Where(p => p.SupportedCountryCodes.Count == 0 || p.SupportedCountryCodes.Contains(country))
			.ToList();

		if (applicable.Count == 0)
		{
			_logger.LogDebug("Incident sync for {Country}: no feed configured — skipped.", country);
			return Result.Success();
		}

		_logger.LogInformation("Incident sync started for country {Country} with {Providers} provider(s).", country, applicable.Count);

		// Resilient parallel fetch — one provider failing does not abort the others.
		// Records are kept paired with their source provider to correctly derive Source.
		var (providerRecordPairs, hadProviderFailure, failedProviders) = await FetchFromApplicableProvidersAsync(applicable, country, ct);

		_logger.LogInformation("Incident sync for {Country}: {Count} records fetched.", country, providerRecordPairs.Count);

		var inserted = 0;
		var updated = 0;
		var skipped = 0;

		foreach (var (record, source) in providerRecordPairs)
		{
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, source, country, ct);

			if (existing is null)
			{
				var incidentResult = Incident.Create(record.ExternalId, source, country, record.Latitude, record.Longitude, record.IncidentType, record.Severity, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				if (incidentResult.IsFailure)
				{
					_logger.LogWarning("Incident sync for {Country}: failed to create incident {ExternalId} — {Error}.", country, record.ExternalId, incidentResult.Error);
					skipped++;
					continue;
				}
				await _repository.AddAsync(incidentResult.Value, ct);
				inserted++;
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude, record.IncidentType, record.Severity, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.UpdateAsync(existing, ct);
				updated++;
			}
		}

		// Purge expired incidents after each sync pass.
		await _repository.DeleteExpiredAsync(ct);

		// Single commit for the entire country sync — all inserts, updates and expiry deletes in one atomic round-trip.
		var saveResult = await _unitOfWork.SaveChangesAsync(ct);
		if (saveResult.IsFailure)
			return saveResult;

		_logger.LogInformation("Incident sync for {Country} complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped.", country, inserted, updated, skipped);

		// Propagate provider failures so Hangfire marks the per-country job as failed.
		if (hadProviderFailure)
			return Result.Failure(new Error("ProviderError", $"Incident sync for {country} had failing provider(s): {string.Join(", ", failedProviders)}."));

		return Result.Success();
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Fetches incident records from each applicable provider individually so that a single
	/// failing provider (HTTP timeout, malformed DATEX II feed, etc.) does not cancel records
	/// from the remaining providers. Returns records paired with their originating provider name
	/// so that Source is set correctly per record rather than defaulting to the first provider.
	/// </summary>
	/// <param name="providers">Providers that support the target country.</param>
	/// <param name="country">ISO 3166-1 alpha-2 country code.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A tuple of: the flat list of (record, sourceName) pairs from all providers that succeeded, a flag indicating whether any provider failed, and the list of failed provider names.</returns>
	private async Task<(List<(IncidentRecord Record, string Source)> Records, bool HadFailure, List<string> FailedProviders)> FetchFromApplicableProvidersAsync(List<IIncidentFeedProvider> providers, string country, CancellationToken ct)
	{
		var failedProviders = new List<string>();

		var tasks = providers.Select(async provider =>
		{
			try
			{
				var records = await provider.FetchAsync(country, ct);
				return (Records: records.Select(r => (Record: r, Source: provider.ProviderName)), Failed: false, ProviderName: provider.ProviderName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Incident provider {Provider} failed for country {Country}.", provider.ProviderName, country);
				return (Records: Enumerable.Empty<(IncidentRecord, string)>(), Failed: true, ProviderName: provider.ProviderName);
			}
		}).ToList();

		var results = await Task.WhenAll(tasks);

		foreach (var result in results.Where(r => r.Failed))
			failedProviders.Add(result.ProviderName);

		var records = results.SelectMany(r => r.Records).ToList();
		return (records, failedProviders.Count > 0, failedProviders);
	}
	#endregion
}