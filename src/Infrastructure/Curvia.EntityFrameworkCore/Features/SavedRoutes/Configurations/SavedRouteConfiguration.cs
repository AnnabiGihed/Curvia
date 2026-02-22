using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="SavedRoute"/> aggregate.
/// </summary>
internal sealed class SavedRouteConfiguration : IEntityTypeConfiguration<SavedRoute>
{
	#region IEntityTypeConfiguration<SavedRoute>
	/// <summary>
	/// Configures EF Core mapping for <see cref="SavedRoute"/>:
	/// table name, primary key, value object conversions, indexes, soft-delete query filter,
	/// foreign key to <see cref="User"/>, owned reviews collection, and route cross-aggregate reference.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="SavedRoute"/>.</param>
	public void Configure(EntityTypeBuilder<SavedRoute> builder)
	{
		builder.ToTable(DbTableNames.SavedRoutes);

		#region Name (VO)
		builder.Property(x => x.Name)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => RouteName.FromPersistence(raw))
			.HasColumnName("Name")
			.HasMaxLength(200);
		#endregion

		#region Visibility
		builder.Property(x => x.Visibility)
			.IsRequired()
			.HasConversion<int>()
			.HasColumnName("Visibility");

		builder.HasIndex(x => x.Visibility)
			.HasDatabaseName("IX_SavedRoutes_Visibility");
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

		#region Primary key
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, value => new SavedRouteId(value));
		#endregion

		#region Notes (nullable VO)
		builder.Property(x => x.Notes)
			.HasConversion(vo => vo != null ? vo.Value : null, raw => raw != null ? RouteNotes.FromPersistence(raw) : null)
			.HasColumnName("Notes")
			.HasMaxLength(1000);
		#endregion

		#region Foreign key — User (with navigation for referential integrity)
		builder.Property(x => x.UserId)
			.IsRequired()
			.HasConversion(id => id.Value, value => new UserId(value))
			.HasColumnName("UserId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_SavedRoutes_AppUsers_UserId");
		#endregion

		#region Reviews (owned entity collection → separate RouteReviews table)
		builder.Navigation(x => x.Reviews)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.OwnsMany(x => x.Reviews, RouteReviewConfiguration.Configure);
		#endregion

		#region RouteId — cross-aggregate reference (strongly typed, no navigation)
		builder.Property(x => x.RouteId)
			.IsRequired()
			.HasConversion(id => id.Value, value => new RouteId(value))
			.HasColumnName("RouteId");

		builder.HasIndex(x => x.RouteId)
			.HasDatabaseName("IX_SavedRoutes_RouteId");

		builder.HasIndex(x => new { x.UserId, x.RouteId })
			.IsUnique()
			.HasDatabaseName("UX_SavedRoutes_UserId_RouteId");
		#endregion
	}
	#endregion
}