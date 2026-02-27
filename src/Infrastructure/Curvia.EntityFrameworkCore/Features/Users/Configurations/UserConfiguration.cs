using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Users.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="User"/>.
///              Maps all value objects to their underlying scalar columns.
///
///              Primitive-obsession fix applied:
///                - <see cref="KeycloakSubject"/> → NVARCHAR(36) KeycloakId column (unique index)
///                - <see cref="UserEmail"/> → NVARCHAR(320) Email column (unique index)
///                - <see cref="DisplayName"/> → NVARCHAR(100) DisplayName column
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
	/// <summary>
	/// Configures EF Core mapping for <see cref="User"/>.
	/// </summary>
	/// <param name="builder">Entity type builder.</param>
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable(DbTableNames.AppUsers);

		#region Primary key
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, v => new UserId(v));
		#endregion

		#region KeycloakId (VO)
		builder.Property(x => x.KeycloakId)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => KeycloakSubject.FromPersistence(raw))
			.HasMaxLength(KeycloakSubject.MaxLength)
			.HasColumnName("KeycloakId");
		#endregion

		#region Email (VO)
		builder.Property(x => x.Email)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => UserEmail.FromPersistence(raw))
			.HasMaxLength(UserEmail.MaxLength)
			.HasColumnName("Email");
		#endregion

		#region DisplayName (VO)
		builder.Property(x => x.DisplayName)
			.IsRequired()
			.HasConversion(vo => vo.Value, raw => DisplayName.FromPersistence(raw))
			.HasMaxLength(DisplayName.MaxLength)
			.HasColumnName("DisplayName");
		#endregion

		#region Locale (primitive nullable string)
		builder.Property(x => x.Locale)
			.HasConversion(
				vo => vo != null ? vo.Value : null,
				raw => raw != null ? Locale.FromPersistence(raw) : null)
			.HasMaxLength(10) // or the constant 10 if not exposed
			.HasColumnName("Locale")
			.IsRequired(false);
		#endregion

		#region Indexes
		builder.HasIndex(x => x.KeycloakId).IsUnique().HasDatabaseName("UX_AppUsers_KeycloakId");
		builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_AppUsers_Email");
		#endregion
	}
}