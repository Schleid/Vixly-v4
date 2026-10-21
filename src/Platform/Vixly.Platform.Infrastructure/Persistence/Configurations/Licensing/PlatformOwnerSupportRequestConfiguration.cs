using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Licensing;

// =========================================================
// FILE: PlatformOwnerSupportRequestConfiguration.cs
//
// PURPOSE:
// Maps owner support requests: recovery and second-factor reset by Vixly support (D-O13).
//
// RESPONSIBILITIES:
// - Map to OwnerSupportRequests, indexed by tenant and status
//
// IMPORTANT NOTES:
// - Status changes with a row version; the history is in the audit and the device events
// =========================================================
internal sealed class PlatformOwnerSupportRequestConfiguration : IEntityTypeConfiguration<PlatformOwnerSupportRequest>
{
    public void Configure(EntityTypeBuilder<PlatformOwnerSupportRequest> builder)
    {
        builder.ToTable("OwnerSupportRequests");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Reason).HasMaxLength(PlatformLicensingLimits.SupportTextMaxLength).IsRequired();
        builder.Property(item => item.IdentityCheck).HasMaxLength(PlatformLicensingLimits.SupportTextMaxLength).IsRequired();
        builder.Property(item => item.DecisionNote).HasMaxLength(PlatformLicensingLimits.SupportTextMaxLength);
        builder.Ignore(item => item.IsOpen);
        builder.HasIndex(item => new { item.PlatformTenantId, item.Status });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformDevice>().WithMany().HasForeignKey(item => item.ReplaceDeviceId).OnDelete(DeleteBehavior.Restrict);
    }
}
