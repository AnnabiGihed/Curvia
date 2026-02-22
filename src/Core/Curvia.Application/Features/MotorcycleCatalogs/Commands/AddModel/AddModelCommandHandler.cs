using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddModel;

internal sealed class AddModelCommandHandler : ICommandHandler<AddModelCommand, ModelDto>
{
	#region Fields
	private readonly ICacheService _cache;
	private readonly IMotorcycleMakerRepository _makers;
	private readonly IMotorcycleCatalogModelRepository _models;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="AddModelCommandHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to resolve the owning maker.</param>
	/// <param name="models">Repository used to validate and persist catalog models.</param>
	/// <param name="cache">Cache service used to invalidate catalog caches.</param>
	public AddModelCommandHandler(IMotorcycleMakerRepository makers, IMotorcycleCatalogModelRepository models, ICacheService cache)
	{
		_cache = cache;
		_makers = makers;
		_models = models;
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Adds a new official motorcycle model under a maker after validating maker existence
	/// and ensuring no duplicate official name exists for that maker.
	/// </summary>
	/// <param name="cmd">Command containing maker, model data, and actor information.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// Success with a <see cref="ModelDto"/> when created; otherwise a failure result
	/// (NotFound when maker does not exist, Conflict when duplicate, or domain failure).
	/// </returns>
	public async Task<Result<ModelDto>> Handle(AddModelCommand cmd, CancellationToken ct)
	{
		var makerId = new MotorcycleMakerId(cmd.MakerId);

		var maker = await _makers.FindByIdAsync(makerId, ct);
		if (maker is null)
			return Result.Failure<ModelDto>(new Error("MotorcycleCatalogModel.MakerNotFound", "The specified maker does not exist."), ResultExceptionType.NotFound);

		if (await _models.OfficialNameExistsAsync(makerId, cmd.Name, ct))
			return Result.Failure<ModelDto>(new Error("MotorcycleCatalogModel.Duplicate", $"An official model named '{cmd.Name}' already exists for this maker."), ResultExceptionType.Conflict);

		var result = MotorcycleCatalogModel.Create(makerId, cmd.Name, cmd.Category, cmd.ActorId);
		if (result.IsFailure)
			return Result.Failure<ModelDto>(result.Error);

		await _models.AddAsync(result.Value, ct);
		await InvalidateModelCache(cmd.MakerId, ct);

		var m = result.Value;
		return Result.Success(new ModelDto(m.Id.Value, m.MakerId.Value, m.Name, m.Category));
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Invalidates the cache entry for official models of the given maker.
	/// </summary>
	/// <param name="makerId">Maker identifier used in the cache key.</param>
	/// <param name="ct">Cancellation token.</param>
	private Task InvalidateModelCache(Guid makerId, CancellationToken ct)
	{
		return _cache.RemoveAsync($"catalog:models:official:{makerId}", ct);
	}
	#endregion
}