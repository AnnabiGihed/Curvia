using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialMakers;

internal sealed class GetOfficialMakersQueryHandler : IQueryHandler<GetOfficialMakersQuery, IReadOnlyList<MakerDto>>
{
	private const string CacheKey = "catalog:makers:official";
	private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

	private readonly ICacheService _cache;
	private readonly IMotorcycleMakerRepository _makers;


	public GetOfficialMakersQueryHandler(IMotorcycleMakerRepository makers, ICacheService cache)
	{
		_makers = makers;
		_cache = cache;
	}

	public async Task<Result<IReadOnlyList<MakerDto>>> Handle(
		GetOfficialMakersQuery query, CancellationToken ct)
	{
		var cached = await _cache.GetAsync<List<MakerDto>>(CacheKey, ct);
		if (cached is not null)
			return Result.Success<IReadOnlyList<MakerDto>>(cached);

		var makers = await _makers.GetAllOfficialAsync(ct);
		var dtos = makers.Select(m => new MakerDto(m.Id.Value, m.Name)).ToList();

		await _cache.SetAsync(CacheKey, dtos, CacheTtl, ct);
		return Result.Success<IReadOnlyList<MakerDto>>(dtos);
	}
}