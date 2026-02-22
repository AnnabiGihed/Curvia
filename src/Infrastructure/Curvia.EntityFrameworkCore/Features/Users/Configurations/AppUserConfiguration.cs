using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Users.Configurations;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="User"/> aggregate.
///
///              ALL value object properties are mapped with HasConversion — each VO wraps a
///              single primitive and maps to a single column. HasConversion is preferable over
///              OwnsOne for single-property VOs: no shadow FK, no extra table join hint,
///              no "owned navigation" noise in EF metadata, and column naming is explicit.
///
///              FromPersistence() on each VO bypasses domain validation for EF materialization.
///              Raw values coming from the DB are assumed valid (they were validated on write).
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable(DbTableNames.AppUsers);

		#region Primary key

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new UserId(value));

		#endregion

		#region KeycloakId — unique identity bridge to Keycloak

		builder.Property(x => x.KeycloakId)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => KeycloakId.FromPersistence(raw))
			.HasColumnName("KeycloakId")
			.HasMaxLength(128);

		builder.HasIndex(x => x.KeycloakId)
			.IsUnique()
			.HasDatabaseName("UX_AppUsers_KeycloakId");

		#endregion

		#region Email

		builder.Property(x => x.Email)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => UserEmail.FromPersistence(raw))
			.HasColumnName("Email")
			.HasMaxLength(320);

		builder.HasIndex(x => x.Email)
			.IsUnique()
			.HasDatabaseName("UX_AppUsers_Email");

		#endregion

		#region DisplayName

		builder.Property(x => x.DisplayName)
			.IsRequired()
			.HasConversion(
				vo => vo.Value,
				raw => DisplayName.FromPersistence(raw))
			.HasColumnName("DisplayName")
			.HasMaxLength(100);

		#endregion

		#region Locale (nullable VO)

		builder.Property(x => x.Locale)
			.HasConversion(
				vo => vo != null ? vo.Value : null,
				raw => raw != null ? Locale.FromPersistence(raw) : null)
			.HasColumnName("Locale")
			.HasMaxLength(10);

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