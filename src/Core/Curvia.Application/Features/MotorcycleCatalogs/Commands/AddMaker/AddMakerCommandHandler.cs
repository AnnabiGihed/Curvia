using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Infrastructure.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.AddMaker;

internal sealed class AddMakerCommandHandler : ICommandHandler<AddMakerCommand, MakerDto>
{
	#region Fields
	private readonly ICacheService _cache;
	private readonly IMotorcycleMakerRepository _makers;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="AddMakerCommandHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to validate and persist makers.</param>
	/// <param name="cache">Cache service used to invalidate catalog caches.</param>
	public AddMakerCommandHandler(IMotorcycleMakerRepository makers, ICacheService cache)
	{
		_cache = cache;
		_makers = makers;
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Adds a new official motorcycle maker after ensuring no duplicate official name exists.
	/// Invalidates the official makers cache upon success.
	/// </summary>
	/// <param name="cmd">Command containing maker name and actor information.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// Success with a <see cref="MakerDto"/> when created; otherwise a failure result
	/// (Conflict when duplicate, or domain failure when validation fails).
	/// </returns>
	public async Task<Result<MakerDto>> Handle(AddMakerCommand cmd, CancellationToken ct)
	{
		if (await _makers.OfficialNameExistsAsync(cmd.Name, ct))
			return Result.Failure<MakerDto>(new Error("MotorcycleMaker.Duplicate", $"An official maker named '{cmd.Name}' already exists."), ResultExceptionType.Conflict);

		var result = MotorcycleMaker.Create(cmd.Name, cmd.ActorId);
		if (result.IsFailure)
			return Result.Failure<MakerDto>(result.Error);

		await _makers.AddAsync(result.Value, ct);
		await InvalidateMakerCache(ct);

		return Result.Success(new MakerDto(result.Value.Id.Value, result.Value.Name));
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Invalidates the cache entry holding the list of official makers.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	private Task InvalidateMakerCache(CancellationToken ct)
	{
		return _cache.RemoveAsync("catalog:makers:official", ct);
	}
	#endregion
}