using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingModels;

internal sealed class GetPendingModelsQueryHandler : IQueryHandler<GetPendingModelsQuery, IReadOnlyList<PendingModelDto>>
{
	#region Fields
	private readonly IMotorcycleMakerRepository _makers;
	private readonly IMotorcycleCatalogModelRepository _models;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="GetPendingModelsQueryHandler"/>.
	/// </summary>
	/// <param name="models">Repository used to load pending model suggestions.</param>
	/// <param name="makers">Repository used to resolve maker names for pending models.</param>
	public GetPendingModelsQueryHandler(IMotorcycleCatalogModelRepository models, IMotorcycleMakerRepository makers)
	{
		_models = models;
		_makers = makers;
	}
	#endregion

	#region IQueryHandler
	/// <summary>
	/// Returns the list of pending models awaiting moderation, enriched with the maker name.
	/// If a maker cannot be resolved (deleted/missing), "Unknown" is used as a fallback.
	/// </summary>
	/// <param name="query">Query parameters (unused for this query).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>List of pending models as <see cref="PendingModelDto"/>.</returns>
	public async Task<Result<IReadOnlyList<PendingModelDto>>> Handle(GetPendingModelsQuery query, CancellationToken ct)
	{
		var models = await _models.GetAllPendingAsync(ct);

		var makerIds = models.Select(m => m.MakerId).Distinct().ToList();
		var makerDict = new Dictionary<MotorcycleMakerId, string>();

		foreach (var id in makerIds)
		{
			var maker = await _makers.FindByIdAsync(id, ct);
			if (maker is not null)
				makerDict[id] = maker.Name;
		}

		var dtos = models.Select(m => new PendingModelDto(m.Id.Value, m.MakerId.Value, makerDict.GetValueOrDefault(m.MakerId, "Unknown"), m.Name, m.Category, m.SuggestedByUserId?.Value, m.Audit.CreatedOnUtc)).ToList();

		return Result.Success<IReadOnlyList<PendingModelDto>>(dtos);
	}
	#endregion
}