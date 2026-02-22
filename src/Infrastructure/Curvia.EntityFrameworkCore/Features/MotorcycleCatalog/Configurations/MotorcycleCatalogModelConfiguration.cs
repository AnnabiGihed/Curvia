using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for the MotorcycleCatalogModel aggregate root.
/// </summary>
internal sealed class MotorcycleCatalogModelConfiguration
	: IEntityTypeConfiguration<MotorcycleCatalogModel>
{
	public void Configure(EntityTypeBuilder<MotorcycleCatalogModel> builder)
	{
		builder.ToTable(DbTableNames.MotorcycleModels);

		#region Primary key

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new MotorcycleCatalogModelId(v));

		#endregion

		#region MakerId (cross-aggregate reference — no navigation)

		builder.Property(x => x.MakerId)
			.IsRequired()
			.HasConversion(id => id.Value, v => new MotorcycleMakerId(v))
			.HasColumnName("MakerId");

		// FK declared explicitly so EF generates the constraint.
		// No navigation property on either side — aggregates reference by ID only.
		builder.HasOne<MotorcycleMaker>()
			.WithMany()
			.HasForeignKey(x => x.MakerId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_MotorcycleModels_MotorcycleMakers_MakerId");

		builder.HasIndex(x => x.MakerId)
			.HasDatabaseName("IX_MotorcycleModels_MakerId");

		#endregion

		#region Name

		builder.Property(x => x.Name)
			.IsRequired()
			.HasMaxLength(150);

		// Unique official name per maker enforced at application layer (OfficialNameExistsAsync).
		// No DB unique index here because PendingValidation models may have temporary duplicates.

		#endregion

		#region Category

		builder.Property(x => x.Category)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30);

		#endregion

		#region Status

		builder.Property(x => x.Status)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30);

		builder.HasIndex(x => x.Status)
			.HasDatabaseName("IX_MotorcycleModels_Status");

		#endregion

		#region SuggestedByUserId (nullable FK — no navigation)

		builder.Property(x => x.SuggestedByUserId)
			.HasConversion(
				id => id != null ? id.Value : (Guid?)null,
				v => v.HasValue ? new UserId(v.Value) : null)
			.HasColumnName("SuggestedByUserId");

		#endregion

		#region Audit

		builder.OwnsOne(x => x.Audit, audit =>
		{
			audit.Property(a => a.CreatedOnUtc).HasColumnName("CreatedOnUtc").IsRequired();
			audit.Property(a => a.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(256);
			audit.Property(a => a.ModifiedOnUtc).HasColumnName("ModifiedOnUtc");
			audit.Property(a => a.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(256);
		});

		#endregion

		#region Soft delete

		builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
		builder.Property(x => x.DeletedOnUtc).HasColumnName("DeletedOnUtc");
		builder.Property(x => x.DeletedBy).HasMaxLength(256);
		builder.HasQueryFilter(x => !x.IsDeleted);

		#endregion
	}
}