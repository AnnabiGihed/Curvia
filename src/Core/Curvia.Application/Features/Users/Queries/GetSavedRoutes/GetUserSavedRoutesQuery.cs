using Pivot.Framework.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.Users.Queries.GetSavedRoutes.Responses;

namespace Curvia.Application.Features.Users.Queries.GetSavedRoutes;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Returns all saved routes for the authenticated user,
///              including embedded reviews where present.
/// </summary>
public sealed record GetUserSavedRoutesQuery(Guid UserId) : IQuery<IReadOnlyList<SavedRouteDto>>;