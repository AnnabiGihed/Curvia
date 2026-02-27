using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Motorcycles.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="Motorcycle"/> aggregate.
///              Single-property VOs (Maker, MotorcycleModel, Nickname, ManufactureYear,
///              EngineDisplacement) mapped with HasConversion — one column each,
///              no owned-type overhead.
/// </summary>
internal sealed class MotorcycleConfiguration : IEntityTypeConfiguration<Motorcycle>
{
	#region IEntityTypeConfiguration<Motorcycle>
	/// <summary>
	/// Configures EF Core mapping for <see cref="Motorcycle"/>:
	/// table name, primary key, value object conversions, indexes, soft-delete query filter,
	/// and foreign key to <see cref="User"/>.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="Motorcycle"/>.</param>
	public void Configure(EntityTypeBuilder<Motorcycle> builder)
	{
		builder.ToTable(DbTableNames.Motorcycles);

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new MotorcycleId(v));
		#endregion

		#region Maker (VO)
		builder.Property(x => x.Maker)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => Maker.FromPersistence(raw))
			.HasMaxLength(100)
			.HasColumnName("Maker");
		#endregion

		#region Model (VO)
		builder.Property(x => x.Model)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => MotorcycleModel.FromPersistence(raw))
			.HasMaxLength(150)
			.HasColumnName("Model");
		#endregion

		#region Year (ManufactureYear VO)
		builder.Property(x => x.Year)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => ManufactureYear.FromPersistence(raw))
			.HasColumnName("Year");
		#endregion

		#region EngineCc (nullable EngineDisplacement VO)
		builder.Property(x => x.EngineCc)
			.HasConversion(vo => vo != null ? (int?)vo.Cc : null, raw => raw.HasValue ? EngineDisplacement.FromPersistence(raw.Value) : null)
			.HasColumnName("EngineCc");
		#endregion

		#region Nickname (nullable VO)
		builder.Property(x => x.Nickname)
			.HasConversion(vo => vo != null ? vo.Value : null, raw => raw != null ? Nickname.FromPersistence(raw) : null)
			.HasMaxLength(100)
			.HasColumnName("Nickname");
		#endregion

		#region IsDefault
		builder.Property(x => x.IsDefault)
			.IsRequired()
			.HasDefaultValue(false)
			.HasColumnName("IsDefault");
		#endregion

		#region Soft delete
		builder.Property(x => x.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);

		builder.Property(x => x.DeletedOnUtc).HasColumnName("DeletedOnUtc");
		builder.Property(x => x.DeletedBy).HasMaxLength(256);

		builder.HasQueryFilter(x => !x.IsDeleted);
		#endregion

		#region UserId FK (no navigation)
		builder.Property(x => x.UserId)
			.IsRequired()
			.HasConversion(id => id.Value, v => new UserId(v))
			.HasColumnName("UserId");

		builder.HasOne<Curvia.Domain.Features.Users.Aggregate.User>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_Motorcycles_AppUsers_UserId");

		builder.HasIndex(x => x.UserId)
			.HasDatabaseName("IX_Motorcycles_UserId");
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
	}
	#endregion
}