using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestModel;

internal sealed class SuggestModelCommandHandler : ICommandHandler<SuggestModelCommand, ModelDto>
{
	#region Fields
	private readonly IMotorcycleMakerRepository _makers;
	private readonly IMotorcycleCatalogModelRepository _models;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SuggestModelCommandHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to resolve the owning maker.</param>
	/// <param name="models">Repository used to validate and persist model suggestions.</param>
	public SuggestModelCommandHandler(IMotorcycleMakerRepository makers, IMotorcycleCatalogModelRepository models)
	{
		_makers = makers;
		_models = models;
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Suggests a new motorcycle model for moderation under an existing maker.
	/// Rejects if the maker does not exist or if an official model with the same name
	/// already exists for that maker.
	/// </summary>
	/// <param name="cmd">Command containing maker, model data, and suggesting user id.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// Success with a <see cref="ModelDto"/> when the suggestion is stored; otherwise a failure result
	/// (NotFound when maker does not exist, Conflict when already official, or domain failure when validation fails).
	/// </returns>
	public async Task<Result<ModelDto>> Handle(SuggestModelCommand cmd, CancellationToken ct)
	{
		var makerId = new MotorcycleMakerId(cmd.MakerId);

		var maker = await _makers.FindByIdAsync(makerId, ct);
		if (maker is null)
			return Result.Failure<ModelDto>(new Error("MotorcycleCatalogModel.MakerNotFound", "The specified maker does not exist."), ResultExceptionType.NotFound);

		if (await _models.OfficialNameExistsAsync(makerId, cmd.Name, ct))
			return Result.Failure<ModelDto>(new Error("MotorcycleCatalogModel.AlreadyOfficial", $"A model named '{cmd.Name}' is already in the official catalog for this maker."), ResultExceptionType.Conflict);

		var result = MotorcycleCatalogModel.Suggest(makerId, cmd.Name, cmd.Category, new UserId(cmd.UserId));

		if (result.IsFailure)
			return Result.Failure<ModelDto>(result.Error);

		await _models.AddAsync(result.Value, ct);

		var m = result.Value;
		return Result.Success(new ModelDto(m.Id.Value, m.MakerId.Value, m.Name, m.Category));
	}
	#endregion
}