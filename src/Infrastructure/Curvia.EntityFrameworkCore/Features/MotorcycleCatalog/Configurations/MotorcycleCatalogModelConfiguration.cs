using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for the MotorcycleCatalogModel aggregate root.
/// </summary>
internal sealed class MotorcycleCatalogModelConfiguration : IEntityTypeConfiguration<MotorcycleCatalogModel>
{
	#region IEntityTypeConfiguration<MotorcycleCatalogModel>
	/// <summary>
	/// Configures EF Core mapping for <see cref="MotorcycleCatalogModel"/>:
	/// table name, property constraints, enum conversions, soft-delete query filter,
	/// primary key conversion, optional SuggestedByUserId persistence, and MakerId FK setup.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="MotorcycleCatalogModel"/>.</param>
	public void Configure(EntityTypeBuilder<MotorcycleCatalogModel> builder)
	{
		builder.ToTable(DbTableNames.MotorcycleModels);

		#region Name

		builder.Property(x => x.Name)
			.IsRequired()
			.HasMaxLength(150);
		#endregion

		#region Status
		builder.Property(x => x.Status)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30);

		builder.HasIndex(x => x.Status)
			.HasDatabaseName("IX_MotorcycleModels_Status");
		#endregion

		#region Category
		builder.Property(x => x.Category)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30);
		#endregion

		#region Soft delete
		builder.Property(x => x.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);

		builder.Property(x => x.DeletedOnUtc)
			.HasColumnName("DeletedOnUtc");

		builder.Property(x => x.DeletedBy)
			.HasMaxLength(256);

		builder.HasQueryFilter(x => !x.IsDeleted);
		#endregion

		#region Primary key
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new MotorcycleCatalogModelId(v));
		#endregion

		#region SuggestedByUserId (nullable FK — no navigation)
		builder.Property(x => x.SuggestedByUserId)
			.HasConversion(
				id => id != null ? id.Value : (Guid?)null,
				v => v.HasValue ? new UserId(v.Value) : null)
			.HasColumnName("SuggestedByUserId");
		#endregion

		#region MakerId (cross-aggregate reference — no navigation)
		builder.Property(x => x.MakerId)
			.IsRequired()
			.HasConversion(id => id.Value, v => new MotorcycleMakerId(v))
			.HasColumnName("MakerId");

		builder.HasOne<MotorcycleMaker>()
			.WithMany()
			.HasForeignKey(x => x.MakerId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_MotorcycleModels_MotorcycleMakers_MakerId");

		builder.HasIndex(x => x.MakerId)
			.HasDatabaseName("IX_MotorcycleModels_MakerId");
		#endregion
	}
	#endregion
}