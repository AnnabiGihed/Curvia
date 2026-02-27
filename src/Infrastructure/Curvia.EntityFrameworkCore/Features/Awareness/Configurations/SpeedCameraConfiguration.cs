using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="SpeedCamera"/>.
///
///              Notable design decisions:
///                - No soft-delete query filter. Lifecycle is managed by the Worker
///                  via DeleteBySourceAsync + upsert. IsDeleted column exists but
///                  is never used for awareness entities.
///                - Composite index on (CountryCode, Source) accelerates the
///                  Worker's delete-by-source operation.
///                - Composite index on (Latitude, Longitude) accelerates bbox queries.
///                  For production scale with PostGIS, replace with a spatial index.
///                - ExternalId + Source + CountryCode form the natural key used by
///                  the Worker for idempotent upserts.
/// </summary>
internal sealed class SpeedCameraConfiguration : IEntityTypeConfiguration<SpeedCamera>
{
	public void Configure(EntityTypeBuilder<SpeedCamera> builder)
	{
		builder.ToTable(DbTableNames.SpeedCameras);

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new SpeedCameraId(v));

		builder.Property(x => x.ExternalId)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(x => x.Source)
			.IsRequired()
			.HasMaxLength(64);

		builder.Property(x => x.CountryCode)
			.IsRequired()
			.HasMaxLength(2);

		builder.Property(x => x.Latitude)
			.IsRequired()
			.HasColumnType("float");

		builder.Property(x => x.Longitude)
			.IsRequired()
			.HasColumnType("float");

		builder.Property(x => x.SpeedLimitKmh)
			.HasColumnName("SpeedLimitKmh");

		builder.Property(x => x.Direction)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(20);

		builder.Property(x => x.LastSyncedUtc)
			.IsRequired();

		builder.OwnsOne(x => x.Audit, audit =>
		{
			audit.Property(a => a.CreatedOnUtc).HasColumnName("CreatedOnUtc");
			audit.Property(a => a.CreatedBy).HasMaxLength(128).HasColumnName("CreatedBy");
			audit.Property(a => a.ModifiedOnUtc).HasColumnName("ModifiedOnUtc");
			audit.Property(a => a.ModifiedBy).HasMaxLength(128).HasColumnName("ModifiedBy");
		});

		// Soft-delete columns exist (Entity<T> has them) but no query filter —
		// lifecycle is managed by DeleteBySourceAsync, not soft delete.
		builder.Property(x => x.IsDeleted).HasDefaultValue(false);

		// Composite index: supports DeleteBySourceAsync (frequent in sync runs)
		builder.HasIndex(x => new { x.CountryCode, x.Source })
			.HasDatabaseName("IX_SpeedCameras_CountryCode_Source");

		// Natural key index for upsert lookups
		builder.HasIndex(x => new { x.ExternalId, x.Source, x.CountryCode })
			.HasDatabaseName("IX_SpeedCameras_ExternalId_Source_CountryCode");

		// Spatial query index — covers the WHERE Latitude BETWEEN and Longitude BETWEEN pattern
		builder.HasIndex(x => new { x.Latitude, x.Longitude })
			.HasDatabaseName("IX_SpeedCameras_Latitude_Longitude");
	}
}