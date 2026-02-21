using Templates.Core.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Submits or replaces the owner's review on a saved route.
///
///              Idempotent: calling this twice replaces the previous review.
///              Only the user who saved the route can review it.
/// </summary>
public sealed record SubmitReviewCommand(
	/// <summary>Authenticated user's application-level ID.</summary>
	Guid UserId,
	/// <summary>The SavedRoute to review.</summary>
	Guid SavedRouteId,
	/// <summary>1–5 star rating.</summary>
	int Rating,
	/// <summary>Optional free-text comment. Max 2,000 chars.</summary>
	string? Comment = null
) : ICommand<SubmitReviewResponse>;