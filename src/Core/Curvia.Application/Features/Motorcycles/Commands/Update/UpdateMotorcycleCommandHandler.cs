using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Application.Features.Motorcycles.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.Update;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="UpdateMotorcycleCommand"/>.
///
///              Pipeline:
///                1. Load the motorcycle via FindByIdAndUserAsync — enforces ownership.
///                2. Delegate mutation to <see cref="Motorcycle.Update"/> — domain validates inputs.
///                3. Persist via UpdateAsync.
/// </summary>
internal sealed class UpdateMotorcycleCommandHandler : ICommandHandler<UpdateMotorcycleCommand, MotorcycleDto>
{
	#region Dependencies
	private readonly IMotorcycleRepository _motorcycleRepository;
	#endregion

	#region Constructor
	public UpdateMotorcycleCommandHandler(IMotorcycleRepository motorcycleRepository)
	{
		_motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result<MotorcycleDto>> Handle(UpdateMotorcycleCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);
		var motorcycleId = new MotorcycleId(command.MotorcycleId);

		var motorcycle = await _motorcycleRepository.FindByIdAndUserAsync(motorcycleId, userId, cancellationToken);
		if (motorcycle is null)
			return Result.Failure<MotorcycleDto>(new Error("Motorcycle.NotFound", "The motorcycle was not found or does not belong to your garage."), ResultExceptionType.NotFound);

		var updateResult = motorcycle.Update(command.Maker, command.Model, command.Year, command.EngineCc, command.Nickname);
		if (updateResult.IsFailure)
			return Result.Failure<MotorcycleDto>(updateResult.Error, updateResult.ResultExceptionType);

		await _motorcycleRepository.UpdateAsync(motorcycle, cancellationToken);

		return Result.Success(MapToDto(motorcycle));
	}
	#endregion

	#region Private helpers
	private static MotorcycleDto MapToDto(Motorcycle m)
	{
		return new(Id: m.Id.Value, UserId: m.UserId.Value, Maker: m.Maker.Value, Model: m.Model.Value, Year: m.Year, EngineCc: m.EngineCc, Nickname: m.Nickname?.Value, IsDefault: m.IsDefault, AddedAtUtc: m.Audit.CreatedOnUtc);
	}
	#endregion
}