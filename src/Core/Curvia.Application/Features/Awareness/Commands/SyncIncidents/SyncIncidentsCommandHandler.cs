using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncIncidents;

internal sealed class SyncIncidentsCommandHandler : ICommandHandler<SyncIncidentsCommand>
{
	private readonly IEnumerable<IIncidentFeedProvider> _providers;
	private readonly IIncidentRepository _repository;

	public SyncIncidentsCommandHandler(
		IEnumerable<IIncidentFeedProvider> providers,
		IIncidentRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}

	public async Task<Result> Handle(SyncIncidentsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		var applicable = _providers
			.Where(p => p.SupportedCountryCodes.Count == 0
					 || p.SupportedCountryCodes.Contains(country))
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
				var incident = Incident.Create(
					record.ExternalId, source, country,
					record.Latitude, record.Longitude,
					record.IncidentType, record.Severity,
					record.Description,
					record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.AddAsync(incident, ct);
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude,
					record.IncidentType, record.Severity, record.Description,
					record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.UpdateAsync(existing, ct);
			}
		}

		await _repository.DeleteExpiredAsync(ct);

		return Result.Success();
	}
}