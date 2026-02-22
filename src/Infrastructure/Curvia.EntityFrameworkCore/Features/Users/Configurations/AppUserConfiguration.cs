using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Users.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="User"/> aggregate.
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<User>
{
	#region IEntityTypeConfiguration<User>
	/// <summary>
	/// Configures EF Core mapping for <see cref="User"/>:
	/// table name, primary key, value object conversions, indexes, and soft-delete query filter.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="User"/>.</param>
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable(DbTableNames.AppUsers);

		#region Email
		builder.Property(x => x.Email)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => UserEmail.FromPersistence(raw))
			.HasColumnName("Email")
			.HasMaxLength(320);

		builder.HasIndex(x => x.Email)
			.IsUnique()
			.HasDatabaseName("UX_AppUsers_Email");
		#endregion

		#region Primary key
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, value => new UserId(value));
		#endregion

		#region DisplayName
		builder.Property(x => x.DisplayName)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => DisplayName.FromPersistence(raw))
			.HasColumnName("DisplayName")
			.HasMaxLength(100);
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

		#region Locale (nullable VO)
		builder.Property(x => x.Locale)
			.HasConversion(vo => vo != null ? vo.Value : null, raw => raw != null ? Locale.FromPersistence(raw) : null)
			.HasColumnName("Locale")
			.HasMaxLength(10);
		#endregion

		#region KeycloakId — unique identity bridge to Keycloak
		builder.Property(x => x.KeycloakId)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => KeycloakId.FromPersistence(raw))
			.HasColumnName("KeycloakId")
			.HasMaxLength(128);

		builder.HasIndex(x => x.KeycloakId)
			.IsUnique()
			.HasDatabaseName("UX_AppUsers_KeycloakId");
		#endregion
	}
	#endregion
}