using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Application.Features.Motorcycles.Responses;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.Motorcycles.Queries.GetByUser;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetUserMotorcyclesQuery"/>.
///              Returns all active motorcycles for the authenticated user, mapped to flat DTOs.
/// </summary>
internal sealed class GetUserMotorcyclesQueryHandler : IQueryHandler<GetUserMotorcyclesQuery, IReadOnlyList<MotorcycleDto>>
{
	#region Dependencies
	private readonly IMotorcycleRepository _motorcycleRepository;
	#endregion

	#region Constructor
	public GetUserMotorcyclesQueryHandler(IMotorcycleRepository motorcycleRepository)
	{
		_motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
	}
	#endregion

	#region IQueryHandler
	public async Task<Result<IReadOnlyList<MotorcycleDto>>> Handle(GetUserMotorcyclesQuery query, CancellationToken cancellationToken)
	{
		var userId = new UserId(query.UserId);

		var motorcycles = await _motorcycleRepository.GetByUserIdAsync(userId, cancellationToken);

		var dtos = motorcycles
			.Select(m => new MotorcycleDto(Id: m.Id.Value, UserId: m.UserId.Value, Maker: m.Maker.Value, Model: m.Model.Value, Year: m.Year, EngineCc: m.EngineCc, Nickname: m.Nickname?.Value, IsDefault: m.IsDefault, AddedAtUtc: m.Audit.CreatedOnUtc))
			.ToList();

		return Result.Success<IReadOnlyList<MotorcycleDto>>(dtos);
	}
	#endregion
}