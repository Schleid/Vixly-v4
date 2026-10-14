using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformPortalSlugConfiguration.cs
//
// PURPOSE:
// Maps portal slug claims to platform.PortalSlugs (D-T10).
//
// RESPONSIBILITIES:
// - A unique index on Slug: the guarantee that no two tenants or
//   registrations hold one address
// - Row version for optimistic concurrency
//
// IMPORTANT NOTES:
// - Permanent rows are never deleted by the application, so a
//   slug is never reused (D-T11)
// =========================================================
internal sealed class PlatformPortalSlugConfiguration : IEntityTypeConfiguration<PlatformPortalSlug>
{
    public void Configure(EntityTypeBuilder<PlatformPortalSlug> builder)
    {
        builder.ToTable("PortalSlugs");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Slug).HasMaxLength(PlatformTenantLimits.SlugMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Slug).IsUnique();
        builder.HasIndex(item => item.PlatformTenantId);
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformTenantRegistration>().WithMany().HasForeignKey(item => item.PlatformTenantRegistrationId).OnDelete(DeleteBehavior.Restrict);
    }
}
