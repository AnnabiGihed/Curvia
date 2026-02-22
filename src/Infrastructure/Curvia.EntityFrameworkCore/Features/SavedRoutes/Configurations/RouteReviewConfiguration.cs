using Curvia.Domain.Features.SavedRoutes.Entities;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="RouteReview"/> entity.
///
///              RouteReview is an owned entity (Entity&lt;RouteReviewId&gt;) inside the
///              SavedRoute aggregate. It maps to its own RouteReviews table so that:
///                - RouteReviewId is a real PK in the database.
///                - Multiple reviews per SavedRoute are stored as a proper collection.
///                - The full audit trail (Audit_CreatedOnUtc / ModifiedOnUtc) is kept.
///
///              The configuration is registered via OwnsMany inside SavedRouteConfiguration,
///              not as a standalone IEntityTypeConfiguration, because EF requires owned
///              entity collections to be configured through the owner's builder.
///              This file exists to document the column layout separately and can be
///              extracted if SavedRouteConfiguration grows too large.
/// </summary>
internal static class RouteReviewConfiguration
{
	/// <summary>
	/// Configures the RouteReview owned entity collection on the SavedRoute builder.
	/// Called from <see cref="SavedRouteConfiguration.Configure"/>.
	/// </summary>
	public static void Configure(OwnedNavigationBuilder<SavedRoute, RouteReview> review)
	{
		review.ToTable(DbTableNames.RouteReviews);

		// Use RouteReviewId (the domain entity's own Id) as the table PK.
		review.HasKey(r => r.Id);

		review.Property(r => r.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new RouteReviewId(value))
			.HasColumnName("Id");

		// Shadow FK back to SavedRoutes (EF auto-creates "SavedRouteId").
		review.WithOwner()
			.HasForeignKey("SavedRouteId");

		#region ReviewerUserId (FK to AppUsers — reviewer identity)

		// Cross-aggregate reference by ID only — no navigation property to User.
		review.Property(r => r.ReviewerUserId)
			.IsRequired()
			.HasConversion(
				id => id.Value,
				value => new UserId(value))
			.HasColumnName("ReviewerUserId");

		// Unique composite index: one review per (SavedRouteId, ReviewerUserId).
		review.HasIndex("SavedRouteId", nameof(RouteReview.ReviewerUserId))
			.IsUnique()
			.HasDatabaseName("UX_RouteReviews_SavedRouteId_ReviewerUserId");

		#endregion

		#region Rating (VO → int column)

		review.Property(r => r.Rating)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => ReviewRating.FromPersistence(raw))
			.HasColumnName("Rating");

		#endregion

		#region Comment (nullable VO → nvarchar column)

		review.Property(r => r.Comment)
			.HasConversion(
				vo => vo != null ? vo.Value : null,
				raw => raw != null ? ReviewComment.FromPersistence(raw) : null)
			.HasColumnName("Comment")
			.HasMaxLength(2000);

		#endregion

		#region ReviewedAtUtc

		review.Property(r => r.ReviewedAtUtc)
			.IsRequired()
			.HasColumnName("ReviewedAtUtc");

		#endregion

		#region Soft delete

		review.Property(r => r.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false)
			.HasColumnName("IsDeleted");

		review.Property(r => r.DeletedOnUtc)
			.HasColumnName("DeletedOnUtc");

		review.Property(r => r.DeletedBy)
			.HasMaxLength(256)
			.HasColumnName("DeletedBy");

		// Query filter on owned entities must be set on the owner (SavedRoute).
		// Soft-deleted reviews are filtered out by loading only active reviews
		// via the repository — EF does not support HasQueryFilter on owned collections directly.

		#endregion
	}
}