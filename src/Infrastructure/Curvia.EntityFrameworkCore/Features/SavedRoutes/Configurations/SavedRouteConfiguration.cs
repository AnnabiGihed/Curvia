using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Domain.Features.Users.Aggregate;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core entity type configuration for the <see cref="SavedRoute"/> aggregate.
///
///              KEY DESIGN DECISIONS:
///              ─────────────────────────────────────────────────────────────────
///              1. RouteId is a plain Guid column — no EF navigation to Route aggregate.
///                 Cross-aggregate references must be by ID only (DDD rule).
///                 Route geometry is queried separately when the UI needs it.
///
///              2. UserId IS a navigation FK to AppUsers — SavedRoute is within the
///                 Users bounded context boundary (it belongs to a user).
///                 The FK ensures referential integrity at the DB level.
///
///              3. RouteReview is an OWNED TYPE mapped into the same SavedRoutes table.
///                 All review columns are nullable because review is optional.
///                 This avoids a separate ReviewId PK and keeps reads to one table.
///
///              4. Unique index on (UserId, RouteId) prevents saving the same route twice.
///
///              5. Global query filter excludes soft-deleted rows from all queries.
/// </summary>
internal sealed class SavedRouteConfiguration : IEntityTypeConfiguration<SavedRoute>
{
	public void Configure(EntityTypeBuilder<SavedRoute> builder)
	{
		builder.ToTable("SavedRoutes");

		#region Primary key

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new SavedRouteId(value));

		#endregion

		#region Foreign key — AppUser (with navigation)

		builder.Property(x => x.UserId)
			.IsRequired()
			.HasConversion(
				id => id.Value,
				value => new UserId(value))
			.HasColumnName("UserId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict)   // soft-delete AppUser; don't cascade-delete routes
			.HasConstraintName("FK_SavedRoutes_AppUsers_UserId");

		#endregion

		#region Foreign key — Route (plain Guid, no navigation)

		builder.Property(x => x.RouteId)
			.IsRequired()
			.HasColumnName("RouteId");

		// Index for fast "show all saves of a route" queries (community feed, stats).
		builder.HasIndex(x => x.RouteId)
			.HasDatabaseName("IX_SavedRoutes_RouteId");

		#endregion

		#region Unique constraint — no duplicate saves

		builder.HasIndex(x => new { x.UserId, x.RouteId })
			.IsUnique()
			.HasDatabaseName("UX_SavedRoutes_UserId_RouteId");

		#endregion

		#region Profile columns

		builder.Property(x => x.Name)
			.IsRequired()
			.HasMaxLength(200)
			.HasColumnName("Name");

		builder.Property(x => x.Notes)
			.HasMaxLength(1000)
			.HasColumnName("Notes");

		builder.Property(x => x.Visibility)
			.IsRequired()
			.HasConversion<int>()
			.HasColumnName("Visibility");

		// Index for community feed queries (WHERE Visibility = 1 ORDER BY Audit_CreatedOnUtc DESC)
		builder.HasIndex(x => x.Visibility)
			.HasDatabaseName("IX_SavedRoutes_Visibility");

		#endregion

		#region Owned — RouteReview (same table, all nullable)

		builder.OwnsOne(x => x.Review, review =>
		{
			review.Property(r => r.Rating)
				.HasColumnName("Review_Rating");

			review.Property(r => r.Comment)
				.HasMaxLength(2000)
				.HasColumnName("Review_Comment");

			review.Property(r => r.ReviewedAtUtc)
				.HasColumnName("Review_ReviewedAtUtc");
		});

		#endregion

		#region Soft delete
		builder.Property(x => x.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false)
			.HasColumnName("IsDeleted");

		builder.Property(x => x.DeletedOnUtc)
			.HasColumnName("DeletedOnUtc");

		builder.Property(x => x.DeletedBy)
			.HasMaxLength(256)
			.HasColumnName("DeletedBy");

		builder.HasQueryFilter(x => !x.IsDeleted);
		#endregion
	}
}