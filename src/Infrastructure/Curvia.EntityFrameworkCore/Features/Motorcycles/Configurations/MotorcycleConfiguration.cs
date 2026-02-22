using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.ValueObjects;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Motorcycles.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="Motorcycle"/> aggregate.
///              Single-property VOs (Maker, MotorcycleModel, Nickname) mapped with
///              HasConversion — one column each, no owned-type overhead.
/// </summary>
internal sealed class MotorcycleConfiguration : IEntityTypeConfiguration<Motorcycle>
{
	public void Configure(EntityTypeBuilder<Motorcycle> builder)
	{
		builder.ToTable(DbTableNames.Motorcycles);

		#region Primary key

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new MotorcycleId(value));

		#endregion

		#region Foreign key — User

		builder.Property(x => x.UserId)
			.IsRequired()
			.HasConversion(
				id => id.Value,
				value => new UserId(value))
			.HasColumnName("UserId");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("FK_Motorcycles_AppUsers_UserId");

		builder.HasIndex(x => x.UserId)
			.HasDatabaseName("IX_Motorcycles_UserId");

		#endregion

		#region Maker (VO)

		builder.Property(x => x.Maker)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => Maker.FromPersistence(raw))
			.HasColumnName("Maker")
			.HasMaxLength(100);

		#endregion

		#region Model (VO)

		builder.Property(x => x.Model)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => MotorcycleModel.FromPersistence(raw))
			.HasColumnName("Model")
			.HasMaxLength(150);

		#endregion

		#region Year / EngineCc (plain primitives — no VO needed)

		builder.Property(x => x.Year)
			.IsRequired()
			.HasColumnName("Year");

		builder.Property(x => x.EngineCc)
			.HasColumnName("EngineCc");

		#endregion

		#region Nickname (nullable VO)

		builder.Property(x => x.Nickname)
			.HasConversion(
				vo => vo != null ? vo.Value : null,
				raw => raw != null ? Nickname.FromPersistence(raw) : null)
			.HasColumnName("Nickname")
			.HasMaxLength(100);

		#endregion

		#region IsDefault

		builder.Property(x => x.IsDefault)
			.IsRequired()
			.HasDefaultValue(false)
			.HasColumnName("IsDefault");

		builder.HasIndex(x => new { x.UserId, x.IsDefault })
			.HasDatabaseName("IX_Motorcycles_UserId_IsDefault");

		#endregion

		#region Soft delete

		builder.Property(x => x.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false)
			.HasColumnName("IsDeleted");

		builder.Property(x => x.DeletedOnUtc)
			.HasColumnName("DeletedOnUtc");

		builder.Property(x => x.DeletedBy)
			.HasMaxLength(256)
			.HasColumnName("DeletedBy");

		builder.HasQueryFilter(x => !x.IsDeleted);

		#endregion
	}
}