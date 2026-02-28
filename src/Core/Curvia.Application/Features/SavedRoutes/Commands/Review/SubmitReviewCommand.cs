using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.SavedRoutes.Commands.Review.Responses;

namespace Curvia.Application.Features.SavedRoutes.Commands.Review;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Submits or replaces a reviewer's review on a saved route.
///
///              WHO CAN REVIEW:
///              The route owner can always review their own route.
///              Community riders can review any Public saved route.
///              The handler enforces ownership/visibility rules before delegating.
///
///              Idempotent: submitting again by the same user replaces the previous review.
///
///              PRIMITIVES AT COMMAND BOUNDARY:
///              Rating (int) and Comment (string?) are kept as raw primitives.
///              The handler builds ReviewRating and ReviewComment VOs and validates them
///              via the domain — FluentValidation mirrors the domain constraints as a
///              fast-fail layer before the command even reaches the handler.
/// </summary>
public sealed record SubmitReviewCommand(
	/// <summary>Authenticated user's application-level ID — also the reviewer's ID.</summary>
	Guid UserId,
	/// <summary>The SavedRoute being reviewed.</summary>
	Guid SavedRouteId,
	/// <summary>1–5 star rating.</summary>
	int Rating,
	/// <summary>Optional free-text comment. Max 2,000 chars.</summary>
	string? Comment = null
) : ICommand<SubmitReviewResponse>;