using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetUserSavedRoutesQuery"/>.
///              Returns all active saved routes for the authenticated user,
///              with their full review collection mapped to flat DTOs.
/// </summary>
internal sealed class GetUserSavedRoutesQueryHandler : IQueryHandler<GetUserSavedRoutesQuery, IReadOnlyList<SavedRouteDto>>
{
	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="GetUserSavedRoutesQueryHandler"/>.
	/// </summary>
	/// <param name="savedRouteRepository">Repository used to read saved routes for a given user.</param>
	public GetUserSavedRoutesQueryHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region IQueryHandler
	/// <summary>
	/// Loads all saved routes for the requested user and maps them (including non-deleted reviews)
	/// to <see cref="SavedRouteDto"/> records for API consumption.
	/// </summary>
	/// <param name="query">Query containing the authenticated user id.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>All active saved routes with their review DTOs.</returns>
	public async Task<Result<IReadOnlyList<SavedRouteDto>>> Handle(GetUserSavedRoutesQuery query, CancellationToken cancellationToken)
	{
		var userId = new UserId(query.UserId);
		var savedRoutes = await _savedRouteRepository.GetByUserIdAsync(userId, cancellationToken);

		var dtos = savedRoutes
			.Select(sr => new SavedRouteDto(
				SavedRouteId: sr.Id.Value,
				RouteId: sr.RouteId.Value,
				Name: sr.Title.Value,
				Notes: sr.Description?.Value,
				Visibility: sr.Visibility,
				SavedAtUtc: sr.Audit.CreatedOnUtc,
				Reviews: sr.Reviews
					.Where(r => !r.IsDeleted)
					.Select(r => new ReviewDto(
						ReviewId: r.Id.Value,
						ReviewerUserId: r.ReviewerUserId.Value,
						Rating: r.Rating.Value,
						Comment: r.Comment?.Value,
						ReviewedAtUtc: r.ReviewedAtUtc))
					.ToList()))
			.ToList();

		return Result.Success<IReadOnlyList<SavedRouteDto>>(dtos);
	}
	#endregion
}