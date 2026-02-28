using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Infrastructure.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveModel;

internal sealed class ApproveModelCommandHandler : ICommandHandler<ApproveModelCommand>
{
	#region Fields
	private readonly ICacheService _cache;
	private readonly IMotorcycleCatalogModelRepository _models;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="ApproveModelCommandHandler"/>.
	/// </summary>
	/// <param name="models">Repository used to load and persist motorcycle catalog models.</param>
	/// <param name="cache">Cache service used to invalidate catalog caches.</param>
	public ApproveModelCommandHandler(IMotorcycleCatalogModelRepository models, ICacheService cache)
	{
		_cache = cache;
		_models = models;
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Approves the specified motorcycle catalog model and invalidates the corresponding
	/// official models cache for its maker.
	/// </summary>
	/// <param name="cmd">Command containing model id and actor information.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// Success when the model is approved and persisted; otherwise a failure result
	/// (NotFound when model does not exist, or domain failure when approval is invalid).
	/// </returns>
	public async Task<Result> Handle(ApproveModelCommand cmd, CancellationToken ct)
	{
		var model = await _models.FindByIdAsync(new MotorcycleCatalogModelId(cmd.ModelId), ct);
		if (model is null)
			return Result.Failure(new Error("MotorcycleCatalogModel.NotFound", "Model not found."), ResultExceptionType.NotFound);

		var approveResult = model.Approve(cmd.ActorId);
		if (approveResult.IsFailure)
			return approveResult;

		await _models.UpdateAsync(model, ct);
		await _cache.RemoveAsync($"catalog:models:official:{model.MakerId.Value}", ct);
		return Result.Success();
	}
	#endregion
}