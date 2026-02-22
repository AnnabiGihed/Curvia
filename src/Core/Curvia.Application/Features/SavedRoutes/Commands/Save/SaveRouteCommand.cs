using Curvia.Application.Features.SavedRoutes.Commands.Save.Responses;
using Curvia.Domain.Features.SavedRoutes.Enums;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.SavedRoutes.Commands.Save;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Saves a generated route to the authenticated user's profile.
///
///              RouteId is kept as a raw Guid at the command boundary — commands are
///              API-facing DTOs and must not expose domain types. The handler wraps it
///              into the domain's RouteId strongly-typed ID before passing to the aggregate.
/// </summary>
public sealed record SaveRouteCommand(
	/// <summary>Authenticated user's application-level ID (resolved from JWT sub → User.Id).</summary>
	Guid UserId,
	/// <summary>The RouteId returned by POST /api/routes.</summary>
	Guid RouteId,
	/// <summary>User-given name for this route (e.g., "Weekend Ardennes loop"). Max 200 chars.</summary>
	string Name,
	/// <summary>Optional private notes. Max 1,000 chars. Never shown publicly.</summary>
	string? Notes = null,
	/// <summary>Visibility: 0 = Private (default), 1 = Public (community feed).</summary>
	SavedRouteVisibility Visibility = SavedRouteVisibility.Private
) : ICommand<SaveRouteResponse>;