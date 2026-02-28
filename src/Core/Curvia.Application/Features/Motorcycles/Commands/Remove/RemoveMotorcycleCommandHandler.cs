using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.Remove;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="RemoveMotorcycleCommand"/>.
///
///              Pipeline:
///                1. Load the motorcycle via FindByIdAndUserAsync — enforces ownership.
///                2. Call aggregate.Remove() — soft-deletes the motorcycle.
///                3. Persist via UpdateAsync.
/// </summary>
internal sealed class RemoveMotorcycleCommandHandler : ICommandHandler<RemoveMotorcycleCommand>
{
	#region Dependencies
	private readonly IMotorcycleRepository _motorcycleRepository;
	#endregion

	#region Constructor
	public RemoveMotorcycleCommandHandler(IMotorcycleRepository motorcycleRepository)
	{
		_motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result> Handle(RemoveMotorcycleCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);
		var motorcycleId = new MotorcycleId(command.MotorcycleId);

		var motorcycle = await _motorcycleRepository.FindByIdAndUserAsync(motorcycleId, userId, cancellationToken);
		if (motorcycle is null)
			return Result.Failure(new Error("Motorcycle.NotFound", "The motorcycle was not found or does not belong to your garage."), ResultExceptionType.NotFound);

		motorcycle.Remove(command.UserId.ToString());

		await _motorcycleRepository.UpdateAsync(motorcycle, cancellationToken);

		return Result.Success();
	}
	#endregion
}