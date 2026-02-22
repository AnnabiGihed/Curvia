using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestMaker;

internal sealed class SuggestMakerCommandHandler : ICommandHandler<SuggestMakerCommand, MakerDto>
{
	private readonly IMotorcycleMakerRepository _makers;

	public SuggestMakerCommandHandler(IMotorcycleMakerRepository makers)
		=> _makers = makers;

	public async Task<Result<MakerDto>> Handle(SuggestMakerCommand cmd, CancellationToken ct)
	{
		// Guard: don't allow duplicate suggestions of already-official makers
		if (await _makers.OfficialNameExistsAsync(cmd.Name, ct))
			return Result.Failure<MakerDto>(new Error(
				"MotorcycleMaker.AlreadyOfficial",
				$"A maker named '{cmd.Name}' is already in the official catalog. Please select it from the list."),
				ResultExceptionType.Conflict);

		var result = MotorcycleMaker.Suggest(cmd.Name, new UserId(cmd.UserId));
		if (result.IsFailure)
			return Result.Failure<MakerDto>(result.Error);

		await _makers.AddAsync(result.Value, ct);

		// Cache is NOT invalidated here — pending makers are not in the public cache.
		return Result.Success(new MakerDto(result.Value.Id.Value, result.Value.Name));
	}
}