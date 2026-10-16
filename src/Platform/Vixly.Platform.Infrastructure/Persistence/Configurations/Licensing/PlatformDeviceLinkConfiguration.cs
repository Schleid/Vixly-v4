using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Licensing;

// =========================================================
// FILE: PlatformDeviceLinkConfiguration.cs
//
// PURPOSE:
// Maps first-device and recovery links (D-L7, D-L8).
//
// RESPONSIBILITIES:
// - Map to DeviceLinks with a unique token hash
//
// IMPORTANT NOTES:
// - Only the hash is stored
// =========================================================
internal sealed class PlatformDeviceLinkConfiguration : IEntityTypeConfiguration<PlatformDeviceLink>
{
    public void Configure(EntityTypeBuilder<PlatformDeviceLink> builder)
    {
        builder.ToTable("DeviceLinks");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.TokenHash).HasMaxLength(PlatformLicensingLimits.HashLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.PlatformTenantId, item.Kind });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformTenantRegistration>().WithMany().HasForeignKey(item => item.PlatformTenantRegistrationId).OnDelete(DeleteBehavior.Restrict);
    }
}
