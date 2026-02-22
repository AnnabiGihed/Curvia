using Templates.Core.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes.Responses;

namespace Curvia.Application.Features.SavedRoutes.Queries.GetPublicRoutes;

// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Returns a paginated list of public saved routes from all users,
///              ordered by average review rating descending then most recently saved.
///              Used to power the community route discovery feed.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Number of routes per page. Capped at 50 in the handler.</param>
public sealed record GetPublicRoutesQuery(int Page, int PageSize) : IQuery<IReadOnlyList<PublicRouteDto>>;