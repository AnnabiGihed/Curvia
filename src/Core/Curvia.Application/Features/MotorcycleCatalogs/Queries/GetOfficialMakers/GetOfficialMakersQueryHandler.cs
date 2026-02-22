using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialMakers;

internal sealed class GetOfficialMakersQueryHandler : IQueryHandler<GetOfficialMakersQuery, IReadOnlyList<MakerDto>>
{
	#region Constants
	private const string CacheKey = "catalog:makers:official";
	private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
	#endregion

	#region Fields
	private readonly ICacheService _cache;
	private readonly IMotorcycleMakerRepository _makers;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="GetOfficialMakersQueryHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to load official makers.</param>
	/// <param name="cache">Cache service used to read/write maker lists.</param>
	public GetOfficialMakersQueryHandler(IMotorcycleMakerRepository makers, ICacheService cache)
	{
		_cache = cache;
		_makers = makers;
	}
	#endregion

	#region IQueryHandler
	/// <summary>
	/// Returns the official maker list, using a 24h cache to avoid repeated DB queries.
	/// </summary>
	/// <param name="query">Query parameters (unused for this query).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>List of official makers as <see cref="MakerDto"/>.</returns>
	public async Task<Result<IReadOnlyList<MakerDto>>> Handle(GetOfficialMakersQuery query, CancellationToken ct)
	{
		var cached = await _cache.GetAsync<List<MakerDto>>(CacheKey, ct);
		if (cached is not null)
			return Result.Success<IReadOnlyList<MakerDto>>(cached);

		var makers = await _makers.GetAllOfficialAsync(ct);
		var dtos = makers.Select(m => new MakerDto(m.Id.Value, m.Name)).ToList();

		await _cache.SetAsync(CacheKey, dtos, CacheTtl, ct);
		return Result.Success<IReadOnlyList<MakerDto>>(dtos);
	}
	#endregion
}