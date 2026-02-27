using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Application.Features.Awareness.Contracts.Providers;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SyncRoadWorksCommand"/>.
///              Fetches road-work records from all applicable providers in parallel,
///              upserts them by natural key (ExternalId + Source + CountryCode),
///              and purges expired records after each run.
/// </summary>
internal sealed class SyncRoadWorksCommandHandler : ICommandHandler<SyncRoadWorksCommand>
{
	#region Dependencies
	/// <summary>All registered road-work data providers.</summary>
	private readonly IEnumerable<IRoadWorkDataProvider> _providers;

	/// <summary>Persistence contract for <see cref="RoadWork"/> aggregates.</summary>
	private readonly IRoadWorkRepository _repository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="SyncRoadWorksCommandHandler"/>.
	/// </summary>
	/// <param name="providers">All registered road-work data providers.</param>
	/// <param name="repository">Persistence contract for road-work aggregates.</param>
	public SyncRoadWorksCommandHandler(IEnumerable<IRoadWorkDataProvider> providers, IRoadWorkRepository repository)
	{
		_providers = providers ?? throw new ArgumentNullException(nameof(providers));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Executes the sync pipeline: filter providers, fetch in parallel, upsert, then delete expired records.
	/// </summary>
	/// <param name="command">Contains the ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A success result when the sync completes without error.</returns>
	public async Task<Result> Handle(SyncRoadWorksCommand command, CancellationToken ct)
	{
		var country = command.CountryCode.ToUpperInvariant();

		// Only invoke providers that support this country
		var applicable = _providers
			.Where(p => p.SupportedCountryCodes.Count == 0 || p.SupportedCountryCodes.Contains(country))
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
				var rwResult = RoadWork.Create(record.ExternalId, source, country, record.Latitude, record.Longitude, record.Title, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				if (rwResult.IsFailure)
					continue;
				await _repository.AddAsync(rwResult.Value, ct);
			}
			else
			{
				existing.Sync(record.Latitude, record.Longitude, record.Title, record.Description, record.ValidFromUtc, record.ValidUntilUtc);
				await _repository.UpdateAsync(existing, ct);
			}
		}

		// Clean up expired records after each sync
		await _repository.DeleteExpiredAsync(ct);

		return Result.Success();
	}
	#endregion

	#region Private helpers
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