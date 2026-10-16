using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Licensing;

// =========================================================
// FILE: PlatformDeviceEventConfiguration.cs
//
// PURPOSE:
// Maps licensing history (D-L14).
//
// RESPONSIBILITIES:
// - Map to DeviceEvents
//
// IMPORTANT NOTES:
// - Append-only (DENY UPDATE, DELETE in the global script)
// =========================================================
internal sealed class PlatformDeviceEventConfiguration : IEntityTypeConfiguration<PlatformDeviceEvent>
{
    public void Configure(EntityTypeBuilder<PlatformDeviceEvent> builder)
    {
        builder.ToTable("DeviceEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventCode).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(item => item.ActorReference).HasMaxLength(PlatformLicensingLimits.ActorReferenceMaxLength);
        builder.Property(item => item.ActorName).HasMaxLength(PlatformLicensingLimits.ActorNameMaxLength);
        builder.Property(item => item.Note).HasMaxLength(500);
        builder.Property(item => item.Arguments).HasConversion(PlatformValueConverters.Arguments, PlatformValueConverters.ArgumentsComparer).IsRequired();
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformDevice>().WithMany().HasForeignKey(item => item.PlatformDeviceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PlatformDeviceId, item.CreatedUtc });
        builder.HasIndex(item => new { item.PlatformTenantId, item.EventCode, item.CreatedUtc });
    }
}
