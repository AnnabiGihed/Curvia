using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.Users.Aggregate;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for the MotorcycleMaker aggregate root.
///
///              WHY NOT HasData:
///              The Entity base class owns an AuditInfo owned type that must be initialised
///              through the domain constructor. HasData bypasses constructors and cannot
///              hydrate owned types reliably — seeding is done via CatalogDataSeeder instead.
/// </summary>
internal sealed class MotorcycleMakerConfiguration : IEntityTypeConfiguration<MotorcycleMaker>
{
	public void Configure(EntityTypeBuilder<MotorcycleMaker> builder)
	{
		builder.ToTable(DbTableNames.MotorcycleMakers);

		#region Primary key

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new MotorcycleMakerId(v));

		#endregion

		#region Name

		builder.Property(x => x.Name)
			.IsRequired()
			.HasMaxLength(100);

		builder.HasIndex(x => x.Name)
			.HasDatabaseName("IX_MotorcycleMakers_Name");

		#endregion

		#region Status

		builder.Property(x => x.Status)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30);

		builder.HasIndex(x => x.Status)
			.HasDatabaseName("IX_MotorcycleMakers_Status");

		#endregion

		#region SuggestedByUserId (nullable FK — no navigation)

		builder.Property(x => x.SuggestedByUserId)
			.HasConversion(
				id => id != null ? id.Value : (Guid?)null,
				v => v.HasValue ? new UserId(v.Value) : null)
			.HasColumnName("SuggestedByUserId");

		#endregion

		#region Audit (owned type from Entity base)

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