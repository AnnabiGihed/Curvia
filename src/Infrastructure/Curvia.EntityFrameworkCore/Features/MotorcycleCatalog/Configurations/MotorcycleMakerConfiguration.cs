using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.ValueObjects;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for the MotorcycleMaker aggregate root.
///              Name is now mapped via the <see cref="MakerName"/> value object.
/// </summary>
internal sealed class MotorcycleMakerConfiguration : IEntityTypeConfiguration<MotorcycleMaker>
{
	#region IEntityTypeConfiguration<MotorcycleMaker>
	/// <summary>
	/// Configures EF Core mapping for <see cref="MotorcycleMaker"/>:
	/// table name, indexes, enum/status conversion, soft-delete query filter,
	/// primary key conversion, MakerName VO conversion, and optional SuggestedByUserId persistence.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="MotorcycleMaker"/>.</param>
	public void Configure(EntityTypeBuilder<MotorcycleMaker> builder)
	{
		builder.ToTable(DbTableNames.MotorcycleMakers);

		#region Name (VO)
		builder.Property(x => x.Name)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => MakerName.FromPersistence(raw))
			.HasMaxLength(MakerName.MaxLength)
			.HasColumnName("Name");

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

		#region Soft delete
		builder.Property(x => x.IsDeleted)
			.IsRequired()
			.HasDefaultValue(false);

		builder.Property(x => x.DeletedOnUtc)
			.HasColumnName("DeletedOnUtc");

		builder.Property(x => x.DeletedBy)
			.HasMaxLength(256);

		builder.HasQueryFilter(x => !x.IsDeleted);
		#endregion

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new MotorcycleMakerId(v));
		#endregion

		#region SuggestedByUserId (nullable FK — no navigation)
		builder.Property(x => x.SuggestedByUserId)
			.HasConversion(id => id != null ? id.Value : (Guid?)null, v => v.HasValue ? new UserId(v.Value) : null)
			.HasColumnName("SuggestedByUserId");
		#endregion
	}
	#endregion
}