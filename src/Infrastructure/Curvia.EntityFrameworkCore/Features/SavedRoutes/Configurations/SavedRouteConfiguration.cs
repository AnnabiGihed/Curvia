using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.SavedRoutes.Aggregate;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Domain.Features.SavedRoutes.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="SavedRoute"/>.
///              Maps all value objects to their underlying scalar columns.
///
///              Primitive-obsession fix applied:
///                - <see cref="SavedRouteTitle"/> → NVARCHAR(150) Title column
///                - <see cref="SavedRouteDescription"/> → NVARCHAR(2000) Description column (nullable)
/// </summary>
internal sealed class SavedRouteConfiguration : IEntityTypeConfiguration<SavedRoute>
{
	/// <summary>
	/// Configures EF Core mapping for <see cref="SavedRoute"/>.
	/// </summary>
	/// <param name="builder">Entity type builder.</param>
	public void Configure(EntityTypeBuilder<SavedRoute> builder)
	{
		builder.ToTable(DbTableNames.SavedRoutes);

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new SavedRouteId(v));
		#endregion

		#region UserId (FK, cross-aggregate reference)
		builder.Property(x => x.UserId)
			.IsRequired()
			.HasConversion(id => id.Value, v => new UserId(v))
			.HasColumnName("UserId");
		#endregion

		#region RouteId (FK, cross-aggregate reference)
		builder.Property(x => x.RouteId)
			.IsRequired()
			.HasConversion(id => id.Value, v => new RouteId(v))
			.HasColumnName("RouteId");
		#endregion

		#region Title (VO)
		builder.Property(x => x.Title)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => SavedRouteTitle.FromPersistence(raw))
			.HasMaxLength(SavedRouteTitle.MaxLength)
			.HasColumnName("Title");
		#endregion

		#region Description (nullable VO)
		builder.Property(x => x.Description)
			.HasConversion(
				vo => vo != null ? vo.Value : null,
				raw => raw != null ? SavedRouteDescription.FromPersistence(raw) : null)
			.HasMaxLength(SavedRouteDescription.MaxLength)
			.HasColumnName("Description");
		#endregion

		#region Visibility (enum)
		builder.Property(x => x.Visibility)
			.IsRequired()
			.HasColumnName("Visibility");
		#endregion

		#region Reviews (owned entity collection)
		builder.Navigation(x => x.Reviews)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.OwnsMany(x => x.Reviews, RouteReviewConfiguration.Configure);
		#endregion

		#region Indexes
		builder.HasIndex(x => x.UserId).HasDatabaseName("IX_SavedRoutes_UserId");
		builder.HasIndex(x => x.RouteId).HasDatabaseName("IX_SavedRoutes_RouteId");
		builder.HasIndex(x => x.Visibility).HasDatabaseName("IX_SavedRoutes_Visibility");
		#endregion
	}
}