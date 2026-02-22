using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Caching.Abstractions;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddMaker;

internal sealed class AddMakerCommandHandler : ICommandHandler<AddMakerCommand, MakerDto>
{
	private readonly IMotorcycleMakerRepository _makers;
	private readonly ICacheService _cache;

	public AddMakerCommandHandler(IMotorcycleMakerRepository makers, ICacheService cache)
	{
		_makers = makers;
		_cache = cache;
	}

	public async Task<Result<MakerDto>> Handle(AddMakerCommand cmd, CancellationToken ct)
	{
		if (await _makers.OfficialNameExistsAsync(cmd.Name, ct))
			return Result.Failure<MakerDto>(new Error(
				"MotorcycleMaker.Duplicate",
				$"An official maker named '{cmd.Name}' already exists."),
				ResultExceptionType.Conflict);

		var result = MotorcycleMaker.Create(cmd.Name, cmd.ActorId);
		if (result.IsFailure)
			return Result.Failure<MakerDto>(result.Error);

		await _makers.AddAsync(result.Value, ct);
		await InvalidateMakerCache(ct);

		return Result.Success(new MakerDto(result.Value.Id.Value, result.Value.Name));
	}

	private Task InvalidateMakerCache(CancellationToken ct)
		=> _cache.RemoveAsync("catalog:makers:official", ct);
}
