namespace Curvia.Domain.Features.MotorcycleCatalog.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Lifecycle status for a maker or model in the motorcycle catalog.
///
///              Official          — visible to all users, seeded or approved by admin.
///              PendingValidation — suggested by a user, awaiting admin approval.
///                                  Not shown in public dropdowns.
/// </summary>
public enum CatalogItemStatus
{
	Official = 0,
	PendingValidation = 1,
}