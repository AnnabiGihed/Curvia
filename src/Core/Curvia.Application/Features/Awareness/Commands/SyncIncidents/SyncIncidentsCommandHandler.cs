using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncIncidents;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncIncidentsCommand"/>.
///              Fetches live traffic incident records from all applicable DATEX II providers
///              in parallel, upserts them by natural key (ExternalId + Source + CountryCode),
///              and purges expired incidents after each run.
///              Runs every 3 minutes per country — optimised for low-latency incident updates.
/// </summary>
internal sealed class SyncIncidentsCommandHandler : ICommandHandler<SyncIncidentsCommand>
{
	#region Dependencies
	/// <summary>All registered incident feed providers.</summary>
	private readonly IEnumerable<IIncidentFeedProvider> _providers;

	/// <summary>Persistence contract for <see cref="Incident"/> aggregates.</summary>
	private readonly IIncidentRepository _repository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncIncidentsCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered incident feed providers.</param>
	/// <param name="repository">Persistence contract for incident aggregates.</param>
	public SyncIncidentsCommandHandler(IEnumerable<IIncidentFeedProvider> providers, IIncidentRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the incident sync pipeline: filter providers, fetch in parallel, upsert, then delete expired records.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A success result when the sync completes; also success when no feed is configured for this country.</returns>
	public async Task<Result> Handle(SyncIncidentsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		var applicable = _providers
			.Where(p => p.SupportedCountryCodes.Count == 0 || p.SupportedCountryCodes.Contains(country))
			.ToList();

		if (applicable.Count == 0)
			return Result.Success(); // No feed configured for this country — not an error

		var fetchTasks = applicable.Select(p => p.FetchAsync(country, ct)).ToList();
		await Task.WhenAll(fetchTasks);
		var records = fetchTasks.SelectMany(t => t.Result).ToList();

		foreach (var record in records)
		{
			var source = applicable.FirstOrDefault()?.ProviderName ?? "datex2";
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, source, country, ct);

			if (existing is null)
			{
				var incidentResult = Incident.Create(record.ExternalId, source, country, record.Latitude, record.Longitude, record.IncidentType, record.Severity, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				if (incidentResult.IsFailure)
					continue;
				await _repository.AddAsync(incidentResult.Value, ct);
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude, record.IncidentType, record.Severity, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.UpdateAsync(existing, ct);
			}
		}

		await _repository.DeleteExpiredAsync(ct);

		return Result.Success();
	}
	#endregion
}