using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.SetDefault;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="SetDefaultMotorcycleCommand"/>.
///
///              Pipeline:
///                1. Load the target motorcycle via FindByIdAndUserAsync — enforces ownership.
///                2. Clear IsDefault on all sibling motorcycles (ClearDefaultAsync) to maintain invariant.
///                3. Call aggregate.SetAsDefault().
///                4. Persist via UpdateAsync.
/// </summary>
internal sealed class SetDefaultMotorcycleCommandHandler : ICommandHandler<SetDefaultMotorcycleCommand>
{
	#region Dependencies
	private readonly IMotorcycleRepository _motorcycleRepository;
	#endregion

	#region Constructor
	public SetDefaultMotorcycleCommandHandler(IMotorcycleRepository motorcycleRepository)
	{
		_motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result> Handle(SetDefaultMotorcycleCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);
		var motorcycleId = new MotorcycleId(command.MotorcycleId);

		var motorcycle = await _motorcycleRepository.FindByIdAndUserAsync(motorcycleId, userId, cancellationToken);
		if (motorcycle is null)
			return Result.Failure(new Error("Motorcycle.NotFound", "The motorcycle was not found or does not belong to your garage."), ResultExceptionType.NotFound);

		await _motorcycleRepository.ClearDefaultAsync(userId, motorcycleId, cancellationToken);

		motorcycle.SetAsDefault();

		await _motorcycleRepository.UpdateAsync(motorcycle, cancellationToken);

		return Result.Success();
	}
	#endregion
}