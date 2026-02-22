using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveMaker;

internal sealed class ApproveMakerCommandHandler : ICommandHandler<ApproveMakerCommand>
{
	private readonly IMotorcycleMakerRepository _makers;
	private readonly ICacheService _cache;

	public ApproveMakerCommandHandler(IMotorcycleMakerRepository makers, ICacheService cache)
	{
		_makers = makers;
		_cache = cache;
	}

	public async Task<Result> Handle(ApproveMakerCommand cmd, CancellationToken ct)
	{
		var maker = await _makers.FindByIdAsync(new MotorcycleMakerId(cmd.MakerId), ct);
		if (maker is null)
			return Result.Failure(new Error(
				"MotorcycleMaker.NotFound", "Maker not found."), ResultExceptionType.NotFound);

		var approveResult = maker.Approve(cmd.ActorId);
		if (approveResult.IsFailure)
			return approveResult;

		await _makers.UpdateAsync(maker, ct);
		await _cache.RemoveAsync("catalog:makers:official", ct);
		return Result.Success();
	}
}