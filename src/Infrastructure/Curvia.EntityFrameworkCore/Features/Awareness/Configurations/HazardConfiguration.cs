using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="Hazard"/>.
///              Maps all value objects (ExternalId, SourceName, AwarenessCountryCode,
///              Coordinate, AwarenessDescription) to their underlying scalar columns.
/// </summary>
internal sealed class HazardConfiguration : IEntityTypeConfiguration<Hazard>
{
	/// <summary>
	/// Configures EF Core mapping for <see cref="Hazard"/>.
	/// </summary>
	/// <param name="builder">Entity type builder.</param>
	public void Configure(EntityTypeBuilder<Hazard> builder)
	{
		builder.ToTable(DbTableNames.Hazards);

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new HazardId(v));
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
		builder.OwnsOne(x => x.Position, pos =>
		{
			pos.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired().HasColumnType("float");
			pos.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired().HasColumnType("float");

			pos.HasIndex(p => new { p.Latitude, p.Longitude })
				.HasDatabaseName("IX_Hazards_Latitude_Longitude");
		});
		#endregion

		#region HazardType
		builder.Property(x => x.HazardType)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30)
			.HasColumnName("HazardType");
		#endregion

		#region Description (nullable VO)
		builder.Property(x => x.Description)
			.HasConversion(vo => vo != null ? vo.Value : null, raw => raw != null ? AwarenessDescription.FromPersistence(raw) : null)
			.HasMaxLength(AwarenessDescription.MaxLength)
			.HasColumnName("Description");
		#endregion

		#region LastSyncedUtc
		builder.Property(x => x.LastSyncedUtc).IsRequired().HasColumnName("LastSyncedUtc");
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

		builder.Property(x => x.IsDeleted).HasDefaultValue(false);

		#region Indexes
		builder.HasIndex("CountryCode", "Source")
			.HasDatabaseName("IX_Hazards_CountryCode_Source");

		builder.HasIndex("ExternalId", "Source", "CountryCode")
			.HasDatabaseName("IX_Hazards_ExternalId_Source_CountryCode");
		#endregion
	}
}