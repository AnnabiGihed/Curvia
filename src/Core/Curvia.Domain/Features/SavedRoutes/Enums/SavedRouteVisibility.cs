namespace Curvia.Domain.Features.SavedRoutes.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Controls the visibility of a saved route.
///              Private — only the owner can see it.
///              Public  — visible to all authenticated users in the community feed.
/// </summary>
public enum SavedRouteVisibility
{
	/// <summary>Only the owner can see this saved route.</summary>
	Private = 0,

	/// <summary>Visible to all users — appears in community route lists.</summary>
	Public = 1
}