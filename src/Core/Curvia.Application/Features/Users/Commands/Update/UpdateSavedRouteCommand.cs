using Curvia.Domain.Features.SavedRoutes.Enums;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.SavedRoutes.Commands.Update;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Updates the name, private notes and/or visibility of a saved route.
///              Only the owner may update their saved route.
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
/// <param name="SavedRouteId">Identifier of the saved route to update.</param>
/// <param name="Name">New route name. Max 200 chars.</param>
/// <param name="Notes">New private notes. Null clears existing notes. Max 1,000 chars.</param>
/// <param name="Visibility">New visibility setting (Public / Private).</param>
public sealed record UpdateSavedRouteCommand(Guid UserId, Guid SavedRouteId, string Name, string? Notes,	SavedRouteVisibility Visibility) : ICommand;