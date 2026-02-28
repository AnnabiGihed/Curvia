using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Application.Features.Motorcycles.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Motorcycles.Commands.Add;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="AddMotorcycleCommand"/>.
///
///              Pipeline:
///                1. Delegate creation to <see cref="Motorcycle.Create"/> — domain validates all inputs.
///                2. If IsDefault, clear the existing default first to maintain the one-default invariant.
///                3. Persist via AddAsync.
/// </summary>
internal sealed class AddMotorcycleCommandHandler : ICommandHandler<AddMotorcycleCommand, MotorcycleDto>
{
	#region Dependencies
	private readonly IMotorcycleRepository _motorcycleRepository;
	#endregion

	#region Constructor
	public AddMotorcycleCommandHandler(IMotorcycleRepository motorcycleRepository)
	{
		_motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
	}
	#endregion

	#region ICommandHandler
	public async Task<Result<MotorcycleDto>> Handle(AddMotorcycleCommand command, CancellationToken cancellationToken)
	{
		var userId = new UserId(command.UserId);

		var motorcycleResult = Motorcycle.Create(userId: userId, maker: command.Maker, model: command.Model, year: command.Year, engineCc: command.EngineCc, nickname: command.Nickname, isDefault: command.IsDefault);

		if (motorcycleResult.IsFailure)
			return Result.Failure<MotorcycleDto>(motorcycleResult.Error, motorcycleResult.ResultExceptionType);

		var motorcycle = motorcycleResult.Value;

		if (command.IsDefault)
			await _motorcycleRepository.ClearDefaultAsync(userId, motorcycle.Id, cancellationToken);

		await _motorcycleRepository.AddAsync(motorcycle, cancellationToken);

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