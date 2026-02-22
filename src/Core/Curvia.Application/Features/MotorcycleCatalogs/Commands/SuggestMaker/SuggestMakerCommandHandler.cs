using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestMaker;

internal sealed class SuggestMakerCommandHandler : ICommandHandler<SuggestMakerCommand, MakerDto>
{
	#region Fields
	private readonly IMotorcycleMakerRepository _makers;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SuggestMakerCommandHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to validate and persist maker suggestions.</param>
	public SuggestMakerCommandHandler(IMotorcycleMakerRepository makers)
	{
		_makers = makers;
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Suggests a new motorcycle maker for moderation.
	/// Rejects if the maker already exists in the official catalog.
	/// </summary>
	/// <param name="cmd">Command containing the suggested maker name and suggesting user id.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// Success with a <see cref="MakerDto"/> when the suggestion is stored; otherwise a failure result
	/// (Conflict when already official, or domain failure when validation fails).
	/// </returns>
	public async Task<Result<MakerDto>> Handle(SuggestMakerCommand cmd, CancellationToken ct)
	{
		if (await _makers.OfficialNameExistsAsync(cmd.Name, ct))
			return Result.Failure<MakerDto>(new Error("MotorcycleMaker.AlreadyOfficial", $"A maker named '{cmd.Name}' is already in the official catalog. Please select it from the list."), ResultExceptionType.Conflict);

		var result = MotorcycleMaker.Suggest(cmd.Name, new UserId(cmd.UserId));
		if (result.IsFailure)
			return Result.Failure<MakerDto>(result.Error);

		await _makers.AddAsync(result.Value, ct);

		return Result.Success(new MakerDto(result.Value.Id.Value, result.Value.Name));
	}
	#endregion
}