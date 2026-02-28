using Curvia.Domain.Features.SavedRoutes.Enums;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Saves a generated route to the authenticated user's profile.
/// </summary>
/// <param name="UserId">Authenticated user identifier (Keycloak-backed AppUser).</param>
/// <param name="RouteId">Generated route identifier to save.</param>
/// <param name="Name">User-given route name (max 200 chars).</param>
/// <param name="Notes">Optional private notes (max 1,000 chars).</param>
/// <param name="Visibility">Route visibility in the community feed (default: Private).</param>
public sealed record SaveRouteCommand(Guid UserId, Guid RouteId, string Name, string? Notes = null, SavedRouteVisibility Visibility = SavedRouteVisibility.Private) : ICommand<SaveRouteResponse>;