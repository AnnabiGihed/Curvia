using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.SavedRoutes.Commands.Unsave;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Removes a saved route from the authenticated user's profile.
///              Ownership is enforced by the handler — only the user who saved the route
///              may unsave it.
///              The underlying aggregate is soft-deleted; it disappears from the user's
///              profile but remains in the database for audit purposes.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
/// <param name="SavedRouteId">Identifier of the saved route to remove.</param>
public sealed record UnsaveRouteCommand(Guid UserId, Guid SavedRouteId) : ICommand;