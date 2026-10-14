using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformRoleConfiguration.cs
//
// PURPOSE:
// Maps the PlatformRole aggregate: platform.Roles and
// platform.RolePermissions.
//
// RESPONSIBILITIES:
// - Unique role names and unique system keys
// - One row per role and permission code
// - Row version for optimistic concurrency
//
// IMPORTANT NOTES:
// - Permission codes are validated by the domain and by the
//   permission catalogue; the database stores them as text
// =========================================================
internal sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(role => role.Name).HasMaxLength(PlatformIdentityLimits.NameMaxLength).IsRequired();
        builder.Property(role => role.SystemKey).HasMaxLength(PlatformIdentityLimits.SystemRoleKeyMaxLength);
        builder.Ignore(role => role.IsSystem);

        builder.HasIndex(role => role.Name).IsUnique();
        builder.HasIndex(role => role.SystemKey).IsUnique().HasFilter("[SystemKey] IS NOT NULL");

        builder.HasMany(role => role.Permissions)
            .WithOne()
            .HasForeignKey(permission => permission.PlatformRoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(role => role.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
