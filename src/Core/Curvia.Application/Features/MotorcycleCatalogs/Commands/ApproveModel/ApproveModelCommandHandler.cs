using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveModel;

internal sealed class ApproveModelCommandHandler : ICommandHandler<ApproveModelCommand>
{
	private readonly IMotorcycleCatalogModelRepository _models;
	private readonly ICacheService _cache;

	public ApproveModelCommandHandler(
		IMotorcycleCatalogModelRepository models, ICacheService cache)
	{
		_models = models;
		_cache = cache;
	}

	public async Task<Result> Handle(ApproveModelCommand cmd, CancellationToken ct)
	{
		var model = await _models.FindByIdAsync(new MotorcycleCatalogModelId(cmd.ModelId), ct);
		if (model is null)
			return Result.Failure(new Error(
				"MotorcycleCatalogModel.NotFound", "Model not found."), ResultExceptionType.NotFound);

		var approveResult = model.Approve(cmd.ActorId);
		if (approveResult.IsFailure)
			return approveResult;

		await _models.UpdateAsync(model, ct);
		await _cache.RemoveAsync($"catalog:models:official:{model.MakerId.Value}", ct);
		return Result.Success();
	}
}