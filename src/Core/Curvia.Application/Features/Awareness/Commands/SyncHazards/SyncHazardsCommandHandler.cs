using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncHazards;

internal sealed class SyncHazardsCommandHandler : ICommandHandler<SyncHazardsCommand>
{
	private readonly IEnumerable<IHazardDataProvider> _providers;
	private readonly IHazardRepository _repository;

	public SyncHazardsCommandHandler(
		IEnumerable<IHazardDataProvider> providers,
		IHazardRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}

	public async Task<Result> Handle(SyncHazardsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		var fetchTasks = _providers.Select(p => p.FetchAsync(country, ct)).ToList();
		await Task.WhenAll(fetchTasks);
		var records = fetchTasks.SelectMany(t => t.Result).ToList();

		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		foreach (var record in records)
		{
			var existing = await _repository.FindByExternalIdAsync(
				record.ExternalId, record.Source(/* workaround — provider sets source */), country, ct);

			// Note: HazardRecord has no Source field — the provider stamps it.
			// In practice, the provider wraps the record in a named source.
			// This is resolved by the OsmHazardProvider prefixing ExternalId with "osm:".
			// For upsert we always delete-then-reload, so no FindByExternalId needed.
		}

		// Simpler strategy for hazards: truncate per source then bulk insert
		foreach (var record in records)
		{
			var hazard = Hazard.Create(
				record.ExternalId,
				DeriveSource(record.ExternalId),
				country,
				record.Latitude,
				record.Longitude,
				record.HazardType,
				record.Description);
			await _repository.AddAsync(hazard, ct);
		}

		return Result.Success();
	}

	private static string DeriveSource(string externalId)
		=> externalId.StartsWith("osm:", StringComparison.OrdinalIgnoreCase) ? "osm" : "unknown";
}