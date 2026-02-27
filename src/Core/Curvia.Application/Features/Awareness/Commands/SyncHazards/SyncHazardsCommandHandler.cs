using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncHazards;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncHazardsCommand"/>.
///              Fetches permanent road-hazard records from all providers in parallel,
///              deletes existing data per source, then bulk-inserts the fresh dataset.
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
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncHazardsCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered hazard data providers.</param>
	/// <param name="repository">Persistence contract for hazard aggregates.</param>
	public SyncHazardsCommandHandler(IEnumerable<IHazardDataProvider> providers, IHazardRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the hazard sync pipeline: fetch in parallel, delete existing per source, then bulk insert.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A success result when the sync completes without error.</returns>
	public async Task<Result> Handle(SyncHazardsCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		var fetchTasks = _providers.Select(p => p.FetchAsync(country, ct)).ToList();
		await Task.WhenAll(fetchTasks);
		var records = fetchTasks.SelectMany(t => t.Result).ToList();

		foreach (var providerName in _providers.Select(p => p.ProviderName).Distinct())
			await _repository.DeleteBySourceAsync(providerName, country, ct);

		// Simpler strategy for hazards: truncate per source then bulk insert
		foreach (var record in records)
		{
			var hazardResult = Hazard.Create(record.ExternalId, DeriveSource(record.ExternalId), country, record.Latitude, record.Longitude, record.HazardType, record.Description);
			if (hazardResult.IsFailure)
				continue;
			await _repository.AddAsync(hazardResult.Value, ct);
		}

		return Result.Success();
	}
	#endregion

	#region Private helpers
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