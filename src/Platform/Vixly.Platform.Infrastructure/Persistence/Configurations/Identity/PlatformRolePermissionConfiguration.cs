using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformRolePermissionConfiguration.cs
//
// PURPOSE:
// Maps role permission codes to platform.RolePermissions.
//
// RESPONSIBILITIES:
// - One row per role and code
// - Index by code for "who has this permission"
//
// IMPORTANT NOTES:
// - Rows are replaced through PlatformRole only
// =========================================================
internal sealed class PlatformRolePermissionConfiguration : IEntityTypeConfiguration<PlatformRolePermission>
{
    public void Configure(EntityTypeBuilder<PlatformRolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.PermissionCode)
            .HasMaxLength(PlatformIdentityLimits.PermissionCodeMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(permission => new { permission.PlatformRoleId, permission.PermissionCode }).IsUnique();
        builder.HasIndex(permission => permission.PermissionCode);
    }
}
