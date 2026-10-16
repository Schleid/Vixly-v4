using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformTenantCardProfileConfiguration.cs
//
// PURPOSE:
// The tenant card profile table.
//
// RESPONSIBILITIES:
// - Map PlatformTenantCardProfile to its table, keys and indexes
//
// IMPORTANT NOTES:
// - One profile per tenant
// =========================================================
internal sealed class PlatformTenantCardProfileConfiguration : IEntityTypeConfiguration<PlatformTenantCardProfile>
{
    public void Configure(EntityTypeBuilder<PlatformTenantCardProfile> builder)
    {
        builder.ToTable("TenantCardProfiles");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => item.PlatformTenantId).IsUnique();
        builder.Property(item => item.ProviderCustomerId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ConsentEmail).HasMaxLength(254);
        builder.Property(item => item.ConsentTextVersion).HasMaxLength(PlatformPaymentsLimits.ConsentVersionMaxLength).IsUnicode(false);
        builder.HasIndex(item => item.AutoCharge).HasFilter("[AutoCharge] = 1");
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.AutoChargeOffByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
