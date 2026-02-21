using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Users.Configurations;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core entity type configuration for the <see cref="User"/> aggregate.
///
///              KEY DESIGN DECISIONS:
///              ─────────────────────────────────────────────────────────────────
///              1. KeycloakId has a UNIQUE INDEX — Keycloak guarantees the sub claim
///                 is stable and unique; we enforce the same guarantee in SQL.
///
///              2. Email has a UNIQUE INDEX — prevents duplicate accounts when
///                 just-in-time provisioning races (idempotency guard).
///
///              3. Soft delete columns (IsDeleted, DeletedOnUtc, DeletedBy) are
///                 inherited from Entity&lt;T&gt; and mapped here explicitly so EF picks
///                 them up without convention-based magic.
///
///              4. No navigation to SavedRoute — cross-aggregate FK is on SavedRoutes
///                 table; AppUser never loads routes directly.
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("AppUsers");

		#region Primary key

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new UserId(value));

		#endregion

		#region Identity bridge — KeycloakId
		builder.Property(x => x.KeycloakId)
			.IsRequired()
			.HasMaxLength(128)     // UUID string = 36 chars; 128 gives room for future formats
			.HasColumnName("KeycloakId");

		// Unique index: one AppUser per Keycloak subject claim.
		builder.HasIndex(x => x.KeycloakId)
			.IsUnique()
			.HasDatabaseName("UX_AppUsers_KeycloakId");
		#endregion

		#region Profile columns
		builder.Property(x => x.Email)
			.IsRequired()
			.HasMaxLength(320)
			.HasColumnName("Email");

		// Unique index: prevents duplicate provisioning on concurrent first-logins.
		builder.HasIndex(x => x.Email)
			.IsUnique()
			.HasDatabaseName("UX_AppUsers_Email");

		builder.Property(x => x.DisplayName)
			.IsRequired()
			.HasMaxLength(100)
			.HasColumnName("DisplayName");

		builder.Property(x => x.Locale)
			.HasMaxLength(10)      // BCP-47 e.g. "fr-BE" = 5 chars; 10 is safe
			.HasColumnName("Locale");
		#endregion
	}
}