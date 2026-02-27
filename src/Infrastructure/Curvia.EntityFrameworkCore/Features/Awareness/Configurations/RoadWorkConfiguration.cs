using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="RoadWork"/>.
///              ValidUntilUtc is indexed to support the active-records filter
///              and the periodic DeleteExpiredAsync cleanup.
/// </summary>
internal sealed class RoadWorkConfiguration : IEntityTypeConfiguration<RoadWork>
{
	public void Configure(EntityTypeBuilder<RoadWork> builder)
	{
		builder.ToTable(DbTableNames.RoadWorks);

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new RoadWorkId(v));

		builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(256);
		builder.Property(x => x.Source).IsRequired().HasMaxLength(64);
		builder.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
		builder.Property(x => x.Latitude).IsRequired().HasColumnType("float");
		builder.Property(x => x.Longitude).IsRequired().HasColumnType("float");
		builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
		builder.Property(x => x.Description).HasMaxLength(1000);
		builder.Property(x => x.ValidFromUtc).IsRequired();
		builder.Property(x => x.ValidUntilUtc).IsRequired();
		builder.Property(x => x.LastSyncedUtc).IsRequired();

		builder.OwnsOne(x => x.Audit, audit =>
		{
			audit.Property(a => a.CreatedOnUtc).HasColumnName("CreatedOnUtc");
			audit.Property(a => a.CreatedBy).HasMaxLength(128).HasColumnName("CreatedBy");
			audit.Property(a => a.ModifiedOnUtc).HasColumnName("ModifiedOnUtc");
			audit.Property(a => a.ModifiedBy).HasMaxLength(128).HasColumnName("ModifiedBy");
		});

		builder.Property(x => x.IsDeleted).HasDefaultValue(false);

		builder.HasIndex(x => new { x.CountryCode, x.Source })
			.HasDatabaseName("IX_RoadWorks_CountryCode_Source");

		builder.HasIndex(x => new { x.ExternalId, x.Source, x.CountryCode })
			.HasDatabaseName("IX_RoadWorks_ExternalId_Source_CountryCode");

		builder.HasIndex(x => new { x.Latitude, x.Longitude })
			.HasDatabaseName("IX_RoadWorks_Latitude_Longitude");

		// Supports active-records filter and cleanup queries
		builder.HasIndex(x => x.ValidUntilUtc)
			.HasDatabaseName("IX_RoadWorks_ValidUntilUtc");
	}
}