using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetUserSavedRoutesQuery"/>.
/// </summary>
internal sealed class GetUserSavedRoutesQueryHandler : IQueryHandler<GetUserSavedRoutesQuery, IReadOnlyList<SavedRouteDto>>
{
	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	public GetUserSavedRoutesQueryHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region IQueryHandler
	public async Task<Result<IReadOnlyList<SavedRouteDto>>> Handle(GetUserSavedRoutesQuery query, CancellationToken cancellationToken)
	{
		var userId = new UserId(query.UserId);
		var savedRoutes = await _savedRouteRepository.GetByUserIdAsync(userId, cancellationToken);

		var dtos = savedRoutes
			.Select(sr => new SavedRouteDto(
				SavedRouteId: sr.Id.Value,
				RouteId: sr.RouteId,
				Name: sr.Name,
				Notes: sr.Notes,
				Visibility: sr.Visibility,
				SavedAtUtc: sr.Audit.CreatedOnUtc,
				Review: sr.Review is null ? null : new ReviewDto(
					id: sr.Review.Id.Value,
					Rating: sr.Review.Rating,
					Comment: sr.Review.Comment,
					ReviewedAtUtc: sr.Review.ReviewedAtUtc)))
			.ToList();

		return Result.Success<IReadOnlyList<SavedRouteDto>>(dtos);
	}
	#endregion
}