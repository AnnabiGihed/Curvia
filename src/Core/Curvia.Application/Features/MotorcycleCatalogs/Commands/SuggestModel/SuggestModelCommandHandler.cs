using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Templates.Core.Application.Abstractions.Messaging.Commands;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestModel;

internal sealed class SuggestModelCommandHandler : ICommandHandler<SuggestModelCommand, ModelDto>
{
	private readonly IMotorcycleMakerRepository _makers;
	private readonly IMotorcycleCatalogModelRepository _models;

	public SuggestModelCommandHandler(
		IMotorcycleMakerRepository makers,
		IMotorcycleCatalogModelRepository models)
	{
		_makers = makers;
		_models = models;
	}

	public async Task<Result<ModelDto>> Handle(SuggestModelCommand cmd, CancellationToken ct)
	{
		var makerId = new MotorcycleMakerId(cmd.MakerId);

		// Maker must exist (Official or Pending — user may be pairing with their own suggestion)
		var maker = await _makers.FindByIdAsync(makerId, ct);
		if (maker is null)
			return Result.Failure<ModelDto>(new Error(
				"MotorcycleCatalogModel.MakerNotFound",
				"The specified maker does not exist."),
				ResultExceptionType.NotFound);

		// Guard: don't suggest an already-official model
		if (await _models.OfficialNameExistsAsync(makerId, cmd.Name, ct))
			return Result.Failure<ModelDto>(new Error(
				"MotorcycleCatalogModel.AlreadyOfficial",
				$"A model named '{cmd.Name}' is already in the official catalog for this maker."),
				ResultExceptionType.Conflict);

		var result = MotorcycleCatalogModel.Suggest(
			makerId, cmd.Name, cmd.Category, new UserId(cmd.UserId));

		if (result.IsFailure)
			return Result.Failure<ModelDto>(result.Error);

		await _models.AddAsync(result.Value, ct);

		var m = result.Value;
		return Result.Success(new ModelDto(m.Id.Value, m.MakerId.Value, m.Name, m.Category));
	}
}
