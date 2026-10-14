using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Subscriptions;

// =========================================================
// FILE: PlatformTenantLimitOverrideConfiguration.cs
//
// PURPOSE:
// Maps limit overrides (D-S8).
//
// RESPONSIBILITIES:
// - Map TenantLimitOverrides
//
// IMPORTANT NOTES:
// - At most one open override per tenant and limit (filtered unique index); removed rows stay as history
// =========================================================
internal sealed class PlatformTenantLimitOverrideConfiguration : IEntityTypeConfiguration<PlatformTenantLimitOverride>
{
    public void Configure(EntityTypeBuilder<PlatformTenantLimitOverride> builder)
    {
        builder.ToTable("TenantLimitOverrides");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Reason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength).IsRequired();
        builder.HasIndex(item => new { item.PlatformTenantId, item.Kind }, "IX_TenantLimitOverrides_OneOpen")
            .IsUnique()
            .HasFilter("[RemovedUtc] IS NULL");
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.SetByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.RemovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(item => item.IsOpen);
    }
}
