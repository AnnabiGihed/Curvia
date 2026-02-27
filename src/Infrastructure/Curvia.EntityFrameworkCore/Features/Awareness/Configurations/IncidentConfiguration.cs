using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Awareness.Aggregates;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="Incident"/>.
///              ValidUntilUtc indexed for active filtering and rapid cleanup
///              after each DATEX II sync run.
/// </summary>
internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
	public void Configure(EntityTypeBuilder<Incident> builder)
	{
		builder.ToTable(DbTableNames.Incidents);

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new IncidentId(v));

		builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(512);
		builder.Property(x => x.Source).IsRequired().HasMaxLength(64);
		builder.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
		builder.Property(x => x.Latitude).IsRequired().HasColumnType("float");
		builder.Property(x => x.Longitude).IsRequired().HasColumnType("float");
		builder.Property(x => x.IncidentType).IsRequired().HasConversion<string>().HasMaxLength(30);
		builder.Property(x => x.Severity).IsRequired().HasConversion<string>().HasMaxLength(20);
		builder.Property(x => x.Description).HasMaxLength(500);
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
			.HasDatabaseName("IX_Incidents_CountryCode_Source");

		builder.HasIndex(x => new { x.ExternalId, x.Source, x.CountryCode })
			.HasDatabaseName("IX_Incidents_ExternalId_Source_CountryCode");

		builder.HasIndex(x => new { x.Latitude, x.Longitude })
			.HasDatabaseName("IX_Incidents_Latitude_Longitude");

		builder.HasIndex(x => x.ValidUntilUtc)
			.HasDatabaseName("IX_Incidents_ValidUntilUtc");
	}
}