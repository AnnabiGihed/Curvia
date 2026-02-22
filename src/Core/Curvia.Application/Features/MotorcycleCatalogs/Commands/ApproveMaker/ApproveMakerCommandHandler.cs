using Templates.Core.Domain.Shared;
using Templates.Core.Caching.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveMaker;

/// <summary>
/// Handles <see cref="ApproveMakerCommand"/> by approving a pending motorcycle maker
/// and invalidating the official makers cache.
/// </summary>
internal sealed class ApproveMakerCommandHandler : ICommandHandler<ApproveMakerCommand>
{
	#region Fields
	private readonly ICacheService _cache;
	private readonly IMotorcycleMakerRepository _makers;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="ApproveMakerCommandHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to load and persist motorcycle makers.</param>
	/// <param name="cache">Cache service used to invalidate catalog caches.</param>
	public ApproveMakerCommandHandler(IMotorcycleMakerRepository makers, ICacheService cache)
	{
		_cache = cache;
		_makers = makers;
	}
	#endregion

	#region ICommandHandler
	/// <summary>
	/// Approves the specified maker and clears the official makers cache key.
	/// </summary>
	/// <param name="cmd">Command containing maker id and actor information.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>
	/// Success when the maker is approved and persisted; otherwise a failure result
	/// (NotFound when maker does not exist, or domain failure when approval is invalid).
	/// </returns>
	public async Task<Result> Handle(ApproveMakerCommand cmd, CancellationToken ct)
	{
		var maker = await _makers.FindByIdAsync(new MotorcycleMakerId(cmd.MakerId), ct);
		if (maker is null)
			return Result.Failure(new Error("MotorcycleMaker.NotFound", "Maker not found."), ResultExceptionType.NotFound);

		var approveResult = maker.Approve(cmd.ActorId);
		if (approveResult.IsFailure)
			return approveResult;

		await _makers.UpdateAsync(maker, ct);
		await _cache.RemoveAsync("catalog:makers:official", ct);
		return Result.Success();
	}
	#endregion
}