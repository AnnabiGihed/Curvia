using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingMakers;

internal sealed class GetPendingMakersQueryHandler : IQueryHandler<GetPendingMakersQuery, IReadOnlyList<PendingMakerDto>>
{
	#region Fields
	private readonly IMotorcycleMakerRepository _makers;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="GetPendingMakersQueryHandler"/>.
	/// </summary>
	/// <param name="makers">Repository used to load pending maker suggestions.</param>
	public GetPendingMakersQueryHandler(IMotorcycleMakerRepository makers)
	{
		_makers = makers;
	}
	#endregion

	#region IQueryHandler
	/// <summary>
	/// Returns the list of pending makers awaiting moderation.
	/// </summary>
	/// <param name="query">Query parameters (unused for this query).</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>List of pending makers as <see cref="PendingMakerDto"/>.</returns>
	public async Task<Result<IReadOnlyList<PendingMakerDto>>> Handle(GetPendingMakersQuery query, CancellationToken ct)
	{
		var makers = await _makers.GetAllPendingAsync(ct);

		var dtos = makers.Select(m => new PendingMakerDto(m.Id.Value, m.Name, m.SuggestedByUserId?.Value, m.Audit.CreatedOnUtc)).ToList();

		return Result.Success<IReadOnlyList<PendingMakerDto>>(dtos);
	}
	#endregion
}