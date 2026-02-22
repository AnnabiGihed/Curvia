using Curvia.Domain.Features.Routing.Routes.Aggregate;
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
/// Purpose     : EF Core configuration for <see cref="SavedRoute"/> aggregate.
///
///              KEY DESIGN DECISIONS:
///              ─────────────────────────────────────────────────────────────────
///              1. RouteId is typed as the domain's RouteId strongly-typed ID (not raw Guid).
///                 Cross-aggregate reference: no EF navigation to Route aggregate.
///                 Maps to a plain uniqueidentifier column via HasConversion.
///
///              2. Name and Notes are value objects mapped with HasConversion (single column each).
///
///              3. Reviews are an owned entity collection mapped to the separate RouteReviews
///                 table via OwnsMany. The review mapping is delegated to RouteReviewConfiguration.
///                 Loading the reviews collection requires .Include(x => x.Reviews) in queries.
///
///              4. Unique index on (UserId, RouteId) prevents saving the same route twice.
///
///              5. Global query filter excludes soft-deleted saved routes.
/// </summary>
internal sealed class SavedRouteConfiguration : IEntityTypeConfiguration<SavedRoute>
{
	public void Configure(EntityTypeBuilder<SavedRoute> builder)
	{
		builder.ToTable(DbTableNames.SavedRoutes);

		#region Primary key

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new SavedRouteId(value));

		#endregion

		#region Foreign key — User (with navigation for referential integrity)

		builder.Property(x => x.UserId)
			.IsRequired()
			.HasConversion(
				id => id.Value,
				value => new UserId(value))
			.HasColumnName("UserId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict)      // soft-delete user; don't cascade-delete routes
			.HasConstraintName("FK_SavedRoutes_AppUsers_UserId");

		#endregion

		#region RouteId — cross-aggregate reference (strongly typed, no navigation)

		// RouteId is the domain's RouteId strong type for compile-time safety.
		// At the DB level it is a plain uniqueidentifier column — EF never joins to Routes.
		builder.Property(x => x.RouteId)
			.IsRequired()
			.HasConversion(
				id => id.Value,
				value => new RouteId(value))
			.HasColumnName("RouteId");

		builder.HasIndex(x => x.RouteId)
			.HasDatabaseName("IX_SavedRoutes_RouteId");

		// Prevents saving the same route twice for the same user.
		builder.HasIndex(x => new { x.UserId, x.RouteId })
			.IsUnique()
			.HasDatabaseName("UX_SavedRoutes_UserId_RouteId");

		#endregion

		#region Name (VO)

		builder.Property(x => x.Name)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => RouteName.FromPersistence(raw))
			.HasColumnName("Name")
			.HasMaxLength(200);

		#endregion

		#region Notes (nullable VO)

		builder.Property(x => x.Notes)
			.HasConversion(
				vo => vo != null ? vo.Value : null,
				raw => raw != null ? RouteNotes.FromPersistence(raw) : null)
			.HasColumnName("Notes")
			.HasMaxLength(1000);

		#endregion

		#region Visibility

		builder.Property(x => x.Visibility)
			.IsRequired()
			.HasConversion<int>()
			.HasColumnName("Visibility");

		builder.HasIndex(x => x.Visibility)
			.HasDatabaseName("IX_SavedRoutes_Visibility");

		#endregion

		#region Reviews (owned entity collection → separate RouteReviews table)

		// Use field access so EF writes directly to the backing _reviews list.
		builder.Navigation(x => x.Reviews)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.OwnsMany(x => x.Reviews, RouteReviewConfiguration.Configure);

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