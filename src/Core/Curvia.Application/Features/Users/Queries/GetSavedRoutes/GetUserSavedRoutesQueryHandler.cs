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
///              Returns all active saved routes for the authenticated user,
///              with their full review collection mapped to flat DTOs.
///
///              VO UNWRAPPING:
///              Domain value objects (RouteName, RouteNotes, ReviewRating, ReviewComment)
///              are unwrapped via .Value at the mapping boundary — DTOs expose primitives only.
///
///              SOFT-DELETED REVIEWS:
///              Reviews are filtered to exclude soft-deleted ones after load.
///              EF does not support HasQueryFilter on owned entity collections,
///              so the filter is applied in-memory here.
/// </summary>
internal sealed class GetUserSavedRoutesQueryHandler
	: IQueryHandler<GetUserSavedRoutesQuery, IReadOnlyList<SavedRouteDto>>
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
	public async Task<Result<IReadOnlyList<SavedRouteDto>>> Handle(
		GetUserSavedRoutesQuery query, CancellationToken cancellationToken)
	{
		var userId = new UserId(query.UserId);
		var savedRoutes = await _savedRouteRepository.GetByUserIdAsync(userId, cancellationToken);

		var dtos = savedRoutes
			.Select(sr => new SavedRouteDto(
				SavedRouteId: sr.Id.Value,
				RouteId: sr.RouteId.Value,           // unwrap RouteId VO
				Name: sr.Name.Value,              // unwrap RouteName VO
				Notes: sr.Notes?.Value,            // unwrap RouteNotes? VO
				Visibility: sr.Visibility,
				SavedAtUtc: sr.Audit.CreatedOnUtc,
				Reviews: sr.Reviews
					.Where(r => !r.IsDeleted)             // filter soft-deleted reviews in memory
					.Select(r => new ReviewDto(
						ReviewId: r.Id.Value,
						ReviewerUserId: r.ReviewerUserId.Value,
						Rating: r.Rating.Value,   // unwrap ReviewRating VO
						Comment: r.Comment?.Value, // unwrap ReviewComment? VO
						ReviewedAtUtc: r.ReviewedAtUtc))
					.ToList()))
			.ToList();

		return Result.Success<IReadOnlyList<SavedRouteDto>>(dtos);
	}
	#endregion
}