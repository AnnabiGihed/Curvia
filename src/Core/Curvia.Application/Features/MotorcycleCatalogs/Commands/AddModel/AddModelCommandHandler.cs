using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Caching.Abstractions;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddModel;

internal sealed class AddModelCommandHandler : ICommandHandler<AddModelCommand, ModelDto>
{
	private readonly IMotorcycleMakerRepository _makers;
	private readonly IMotorcycleCatalogModelRepository _models;
	private readonly ICacheService _cache;

	public AddModelCommandHandler(
		IMotorcycleMakerRepository makers,
		IMotorcycleCatalogModelRepository models,
		ICacheService cache)
	{
		_makers = makers;
		_models = models;
		_cache = cache;
	}

	public async Task<Result<ModelDto>> Handle(AddModelCommand cmd, CancellationToken ct)
	{
		var makerId = new MotorcycleMakerId(cmd.MakerId);

		var maker = await _makers.FindByIdAsync(makerId, ct);
		if (maker is null)
			return Result.Failure<ModelDto>(new Error(
				"MotorcycleCatalogModel.MakerNotFound",
				"The specified maker does not exist."),
				ResultExceptionType.NotFound);

		if (await _models.OfficialNameExistsAsync(makerId, cmd.Name, ct))
			return Result.Failure<ModelDto>(new Error(
				"MotorcycleCatalogModel.Duplicate",
				$"An official model named '{cmd.Name}' already exists for this maker."),
				ResultExceptionType.Conflict);

		var result = MotorcycleCatalogModel.Create(makerId, cmd.Name, cmd.Category, cmd.ActorId);
		if (result.IsFailure)
			return Result.Failure<ModelDto>(result.Error);

		await _models.AddAsync(result.Value, ct);
		await InvalidateModelCache(cmd.MakerId, ct);

		var m = result.Value;
		return Result.Success(new ModelDto(m.Id.Value, m.MakerId.Value, m.Name, m.Category));
	}

	private Task InvalidateModelCache(Guid makerId, CancellationToken ct)
		=> _cache.RemoveAsync($"catalog:models:official:{makerId}", ct);
}