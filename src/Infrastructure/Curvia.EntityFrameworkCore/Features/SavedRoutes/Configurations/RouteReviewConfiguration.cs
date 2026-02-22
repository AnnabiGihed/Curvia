using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Entities;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="RouteReview"/> entity.
/// </summary>
internal static class RouteReviewConfiguration
{
	#region Public Methods
	/// <summary>
	/// Configures the owned collection mapping for <see cref="RouteReview"/> within <see cref="SavedRoute"/>.
	/// Defines table mapping, primary key, owner FK, value object conversions, soft-delete columns,
	/// and uniqueness constraint to prevent duplicate reviews by the same reviewer for a given saved route.
	/// </summary>
	/// <param name="review">Owned navigation builder for <see cref="RouteReview"/>.</param>
	public static void Configure(OwnedNavigationBuilder<SavedRoute, RouteReview> review)
	{
		review.ToTable(DbTableNames.RouteReviews);

		#region Primary Key
		review.HasKey(r => r.Id);

		review.Property(r => r.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, value => new RouteReviewId(value))
			.HasColumnName("Id");
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
		#endregion

		#region SaveRoute ForeignKey
		review.WithOwner()
			.HasForeignKey("SavedRouteId");
		#endregion

		#region Rating (VO → int column)
		review.Property(r => r.Rating)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => ReviewRating.FromPersistence(raw))
			.HasColumnName("Rating");
		#endregion

		#region Comment (nullable VO → nvarchar column)
		review.Property(r => r.Comment)
			.HasConversion(vo => vo != null ? vo.Value : null, raw => raw != null ? ReviewComment.FromPersistence(raw) : null)
			.HasColumnName("Comment")
			.HasMaxLength(2000);
		#endregion

		#region ReviewerUserId (FK to AppUsers — reviewer identity)
		review.Property(r => r.ReviewerUserId)
			.IsRequired()
			.HasConversion(id => id.Value, value => new UserId(value))
			.HasColumnName("ReviewerUserId");

		review.HasIndex("SavedRouteId", nameof(RouteReview.ReviewerUserId))
			.IsUnique()
			.HasDatabaseName("UX_RouteReviews_SavedRouteId_ReviewerUserId");
		#endregion
	}
	#endregion
}