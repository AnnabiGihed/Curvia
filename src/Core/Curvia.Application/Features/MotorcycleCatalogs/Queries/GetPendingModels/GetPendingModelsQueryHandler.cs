using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Queries;
using Templates.Core.Domain.Shared;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingModels;

internal sealed class GetPendingModelsQueryHandler : IQueryHandler<GetPendingModelsQuery, IReadOnlyList<PendingModelDto>>
{
	private readonly IMotorcycleCatalogModelRepository _models;
	private readonly IMotorcycleMakerRepository _makers;

	public GetPendingModelsQueryHandler(
		IMotorcycleCatalogModelRepository models,
		IMotorcycleMakerRepository makers)
	{
		_models = models;
		_makers = makers;
	}

	public async Task<Result<IReadOnlyList<PendingModelDto>>> Handle(
		GetPendingModelsQuery query, CancellationToken ct)
	{
		var models = await _models.GetAllPendingAsync(ct);

		// Resolve maker names in one pass
		var makerIds = models.Select(m => m.MakerId).Distinct().ToList();
		var makerDict = new Dictionary<MotorcycleMakerId, string>();

		foreach (var id in makerIds)
		{
			var maker = await _makers.FindByIdAsync(id, ct);
			if (maker is not null)
				makerDict[id] = maker.Name;
		}

		var dtos = models
			.Select(m => new PendingModelDto(
				m.Id.Value,
				m.MakerId.Value,
				makerDict.GetValueOrDefault(m.MakerId, "Unknown"),
				m.Name,
				m.Category,
				m.SuggestedByUserId?.Value,
				m.Audit.CreatedOnUtc))
			.ToList();

		return Result.Success<IReadOnlyList<PendingModelDto>>(dtos);
	}
}