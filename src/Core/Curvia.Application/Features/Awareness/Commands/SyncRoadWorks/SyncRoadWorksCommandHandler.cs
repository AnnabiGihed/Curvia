using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

internal sealed class SyncRoadWorksCommandHandler : ICommandHandler<SyncRoadWorksCommand>
{
	private readonly IEnumerable<IRoadWorkDataProvider> _providers;
	private readonly IRoadWorkRepository _repository;

	public SyncRoadWorksCommandHandler(
		IEnumerable<IRoadWorkDataProvider> providers,
		IRoadWorkRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}

	public async Task<Result> Handle(SyncRoadWorksCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		// Only invoke providers that support this country
		var applicable = _providers
			.Where(p => p.SupportedCountryCodes.Count == 0
					 || p.SupportedCountryCodes.Contains(country))
			.ToList();

		var fetchTasks = applicable.Select(p => p.FetchAsync(country, ct)).ToList();
		await Task.WhenAll(fetchTasks);
		var records = fetchTasks.SelectMany(t => t.Result).ToList();

		// Upsert strategy: find existing by externalId+source+country, update or create
		foreach (var record in records)
		{
			var source = DeriveSource(record.ExternalId, applicable);
			var existing = await _repository.FindByExternalIdAsync(record.ExternalId, source, country, ct);

			if (existing is null)
			{
				var rw = RoadWork.Create(
					record.ExternalId, source, country,
					record.Latitude, record.Longitude,
					record.Title, record.Description,
					record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.AddAsync(rw, ct);
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude,
					record.Title, record.Description,
					record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.UpdateAsync(existing, ct);
			}
		}

		// Clean up expired records after each sync
		await _repository.DeleteExpiredAsync(ct);

		return Result.Success();
	}

	private static string DeriveSource(string externalId, List<IRoadWorkDataProvider> providers)
	{
		if (externalId.StartsWith("osm:", StringComparison.OrdinalIgnoreCase)) return "osm";
		if (externalId.StartsWith("gipod:", StringComparison.OrdinalIgnoreCase)) return "gipod";
		return providers.FirstOrDefault()?.ProviderName ?? "unknown";
	}
}