using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="Incident"/>.
///              Maps all value objects (ExternalId, SourceName, AwarenessCountryCode,
///              Coordinate, AwarenessDescription) to their underlying scalar columns.
/// </summary>
internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
	/// <summary>
	/// Configures EF Core mapping for <see cref="Incident"/>.
	/// </summary>
	/// <param name="builder">Entity type builder.</param>
	public void Configure(EntityTypeBuilder<Incident> builder)
	{
		builder.ToTable(DbTableNames.Incidents);

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new IncidentId(v));
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
				.HasDatabaseName("IX_Incidents_Latitude_Longitude");
		});
		#endregion

		#region IncidentType
		builder.Property(x => x.IncidentType)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(30)
			.HasColumnName("IncidentType");
		#endregion

		#region Severity
		builder.Property(x => x.Severity)
			.IsRequired()
			.HasConversion<string>()
			.HasMaxLength(20)
			.HasColumnName("Severity");
		#endregion

		#region Description (nullable VO)
		builder.Property(x => x.Description)
			.HasConversion(vo => vo != null ? vo.Value : null, raw => raw != null ? AwarenessDescription.FromPersistence(raw) : null)
			.HasMaxLength(AwarenessDescription.MaxLength)
			.HasColumnName("Description");
		#endregion

		#region Validity window
		builder.Property(x => x.ValidFromUtc).IsRequired().HasColumnName("ValidFromUtc");
		builder.Property(x => x.ValidUntilUtc).IsRequired().HasColumnName("ValidUntilUtc");
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
			.HasDatabaseName("IX_Incidents_CountryCode_Source");

		builder.HasIndex("ExternalId", "Source", "CountryCode")
			.HasDatabaseName("IX_Incidents_ExternalId_Source_CountryCode");

		builder.HasIndex(x => x.ValidUntilUtc)
			.HasDatabaseName("IX_Incidents_ValidUntilUtc");
		#endregion
	}
}