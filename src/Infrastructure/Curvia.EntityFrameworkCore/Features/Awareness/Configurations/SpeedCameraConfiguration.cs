using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

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
///                - Value objects (ExternalId, SourceName, AwarenessCountryCode, SpeedLimit,
///                  Coordinate) are mapped with HasConversion — no owned-type overhead.
///                  Coordinate maps to two separate float columns (Latitude, Longitude)
///                  using split conversion helpers.
/// </summary>
internal sealed class SpeedCameraConfiguration : IEntityTypeConfiguration<SpeedCamera>
{
	/// <summary>
	/// Configures EF Core mapping for <see cref="SpeedCamera"/>.
	/// </summary>
	/// <param name="builder">Entity type builder.</param>
	public void Configure(EntityTypeBuilder<SpeedCamera> builder)
	{
		builder.ToTable(DbTableNames.SpeedCameras);

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new SpeedCameraId(v));
		#endregion

		#region ExternalId (VO)
		builder.Property(x => x.ExternalId)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => ExternalId.FromPersistence(raw))
			.HasMaxLength(ExternalId.MaxLength)
			.HasColumnName("ExternalId");
		#endregion

		#region Source (VO)
		builder.Property(x => x.Source)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => SourceName.FromPersistence(raw))
			.HasMaxLength(SourceName.MaxLength)
			.HasColumnName("Source");
		#endregion

		#region CountryCode (VO)
		builder.Property(x => x.CountryCode)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => AwarenessCountryCode.FromPersistence(raw))
			.HasMaxLength(2)
			.HasColumnName("CountryCode");
		#endregion

		#region Position (Coordinate VO — two scalar columns)
		// Coordinate is an owned value object. EF maps it to two float columns on the same table.
		builder.OwnsOne(x => x.Position, pos =>
		{
			pos.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired().HasColumnType("float");
			pos.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired().HasColumnType("float");

			pos.HasIndex(p => new { p.Latitude, p.Longitude })
				.HasDatabaseName("IX_SpeedCameras_Latitude_Longitude");
		});
		#endregion

		#region SpeedLimit (nullable VO)
		builder.Property(x => x.SpeedLimit)
			.HasConversion(vo => vo != null ? (int?)vo.Kmh : null, raw => raw.HasValue ? SpeedLimit.FromPersistence(raw.Value) : null)
			.HasColumnName("SpeedLimitKmh");
		#endregion

		#region Direction
		builder.Property(x => x.Direction)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(20)
			.HasColumnName("Direction");
		#endregion

		#region LastSyncedUtc
		builder.Property(x => x.LastSyncedUtc)
			.IsRequired()
			.HasColumnName("LastSyncedUtc");
		#endregion

		#region Audit
		builder.OwnsOne(x => x.Audit, audit =>
		{
			audit.Property(a => a.CreatedOnUtc).HasColumnName("CreatedOnUtc");
			audit.Property(a => a.CreatedBy).HasMaxLength(128).HasColumnName("CreatedBy");
			audit.Property(a => a.ModifiedOnUtc).HasColumnName("ModifiedOnUtc");
			audit.Property(a => a.ModifiedBy).HasMaxLength(128).HasColumnName("ModifiedBy");
		});
		#endregion

		#region Soft-delete columns (unused for awareness — no query filter)
		builder.Property(x => x.IsDeleted).HasDefaultValue(false);
		#endregion

		#region Indexes
		// Supports DeleteBySourceAsync (frequent in sync runs)
		builder.HasIndex("CountryCode", "Source")
			.HasDatabaseName("IX_SpeedCameras_CountryCode_Source");

		// Natural key for upsert lookups
		builder.HasIndex("ExternalId", "Source", "CountryCode")
			.HasDatabaseName("IX_SpeedCameras_ExternalId_Source_CountryCode");
		#endregion
	}
}