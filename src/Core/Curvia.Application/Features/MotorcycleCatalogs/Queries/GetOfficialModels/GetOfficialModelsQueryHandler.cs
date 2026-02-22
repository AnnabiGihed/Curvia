using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialModels;

internal sealed class GetOfficialModelsQueryHandler : IQueryHandler<GetOfficialModelsQuery, IReadOnlyList<ModelDto>>
{
	#region Constants
	private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
	#endregion

	#region Fields
	private readonly ICacheService _cache;
	private readonly IMotorcycleCatalogModelRepository _models;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="GetOfficialModelsQueryHandler"/>.
	/// </summary>
	/// <param name="models">Repository used to load official models by maker.</param>
	/// <param name="cache">Cache service used to read/write model lists.</param>
	public GetOfficialModelsQueryHandler(IMotorcycleCatalogModelRepository models, ICacheService cache)
	{
		_cache = cache;
		_models = models;
	}
	#endregion

	#region IQueryHandler
	/// <summary>
	/// Returns the official model list for a given maker, using a 24h cache to avoid repeated DB queries.
	/// Cache key is maker-specific: "catalog:models:official:{MakerId}".
	/// </summary>
	/// <param name="query">Query containing the maker identifier.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>List of official models as <see cref="ModelDto"/>.</returns>
	public async Task<Result<IReadOnlyList<ModelDto>>> Handle(GetOfficialModelsQuery query, CancellationToken ct)
	{
		var cacheKey = $"catalog:models:official:{query.MakerId}";

		var cached = await _cache.GetAsync<List<ModelDto>>(cacheKey, ct);
		if (cached is not null)
			return Result.Success<IReadOnlyList<ModelDto>>(cached);

		var makerId = new MotorcycleMakerId(query.MakerId);
		var models = await _models.GetOfficialByMakerAsync(makerId, ct);

		var dtos = models.Select(m => new ModelDto(m.Id.Value, m.MakerId.Value, m.Name, m.Category)).ToList();

		await _cache.SetAsync(cacheKey, dtos, CacheTtl, ct);
		return Result.Success<IReadOnlyList<ModelDto>>(dtos);
	}
	#endregion
}