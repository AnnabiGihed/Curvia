using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Queries;
using Templates.Core.Caching.Abstractions;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialModels;

internal sealed class GetOfficialModelsQueryHandler : IQueryHandler<GetOfficialModelsQuery, IReadOnlyList<ModelDto>>
{
	private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

	private readonly IMotorcycleCatalogModelRepository _models;
	private readonly ICacheService _cache;

	public GetOfficialModelsQueryHandler(
		IMotorcycleCatalogModelRepository models, ICacheService cache)
	{
		_models = models;
		_cache = cache;
	}

	public async Task<Result<IReadOnlyList<ModelDto>>> Handle(
		GetOfficialModelsQuery query, CancellationToken ct)
	{
		var cacheKey = $"catalog:models:official:{query.MakerId}";

		var cached = await _cache.GetAsync<List<ModelDto>>(cacheKey, ct);
		if (cached is not null)
			return Result.Success<IReadOnlyList<ModelDto>>(cached);

		var makerId = new MotorcycleMakerId(query.MakerId);
		var models = await _models.GetOfficialByMakerAsync(makerId, ct);

		var dtos = models
			.Select(m => new ModelDto(m.Id.Value, m.MakerId.Value, m.Name, m.Category))
			.ToList();

		await _cache.SetAsync(cacheKey, dtos, CacheTtl, ct);
		return Result.Success<IReadOnlyList<ModelDto>>(dtos);
	}
}