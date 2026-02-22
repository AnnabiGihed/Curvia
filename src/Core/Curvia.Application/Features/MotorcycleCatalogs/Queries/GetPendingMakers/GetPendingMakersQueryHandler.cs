using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Queries;
using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingMakers;

internal sealed class GetPendingMakersQueryHandler : IQueryHandler<GetPendingMakersQuery, IReadOnlyList<PendingMakerDto>>
{
	private readonly IMotorcycleMakerRepository _makers;

	public GetPendingMakersQueryHandler(IMotorcycleMakerRepository makers)
		=> _makers = makers;

	public async Task<Result<IReadOnlyList<PendingMakerDto>>> Handle(
		GetPendingMakersQuery query, CancellationToken ct)
	{
		var makers = await _makers.GetAllPendingAsync(ct);

		var dtos = makers
			.Select(m => new PendingMakerDto(
				m.Id.Value,
				m.Name,
				m.SuggestedByUserId?.Value,
				m.Audit.CreatedOnUtc))
			.ToList();

		return Result.Success<IReadOnlyList<PendingMakerDto>>(dtos);
	}
}