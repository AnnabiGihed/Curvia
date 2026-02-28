using Pivot.Framework.Domain.Shared;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;
using Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes.Responses;

namespace Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Handles <see cref="GetPublicRoutesQuery"/>.
///              Returns a paginated page of public saved routes for the community feed.
///              Notes are excluded from the response — they are private to the route owner.
///              Reviews are filtered to active (non-deleted) entries only.
/// </summary>
internal sealed class GetPublicRoutesQueryHandler : IQueryHandler<GetPublicRoutesQuery, IReadOnlyList<PublicRouteDto>>
{
	#region Fields
	private const int MaxPageSize = 50;
	#endregion

	#region Dependencies
	private readonly ISavedRouteRepository _savedRouteRepository;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="GetPublicRoutesQueryHandler"/>.
	/// </summary>
	/// <param name="savedRouteRepository">Repository used to load public saved routes.</param>
	public GetPublicRoutesQueryHandler(ISavedRouteRepository savedRouteRepository)
	{
		_savedRouteRepository = savedRouteRepository ?? throw new ArgumentNullException(nameof(savedRouteRepository));
	}
	#endregion

	#region IQueryHandler
	/// <summary>
	/// Loads the requested page of public saved routes and maps them to <see cref="PublicRouteDto"/> records.
	/// Page number is clamped to a minimum of 1 and page size is clamped to [1, <see cref="MaxPageSize"/>].
	/// </summary>
	/// <param name="query">Query containing the requested page and page size.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A page of public routes with their active reviews.</returns>
	public async Task<Result<IReadOnlyList<PublicRouteDto>>> Handle(GetPublicRoutesQuery query, CancellationToken cancellationToken)
	{
		var page = Math.Max(1, query.Page);
		var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
		var skip = (page - 1) * pageSize;

		var routes = await _savedRouteRepository.GetPublicRoutesAsync(skip, pageSize, cancellationToken);

		var dtos = routes
			.Select(sr => new PublicRouteDto(
				SavedRouteId: sr.Id.Value,
				RouteId: sr.RouteId.Value,
				OwnerUserId: sr.UserId.Value,
				Name: sr.Title.Value,
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

		return Result.Success<IReadOnlyList<PublicRouteDto>>(dtos);
	}
	#endregion
}