using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Subscriptions;

// =========================================================
// FILE: PlatformTenantShipmentUsageConfiguration.cs
//
// PURPOSE:
// Maps the monthly shipment counts (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Map TenantShipmentUsage
//
// IMPORTANT NOTES:
// - One row per company and month (unique index); a row version keeps two reports from overwriting each other
// =========================================================
internal sealed class PlatformTenantShipmentUsageConfiguration : IEntityTypeConfiguration<PlatformTenantShipmentUsage>
{
    public void Configure(EntityTypeBuilder<PlatformTenantShipmentUsage> builder)
    {
        builder.ToTable("TenantShipmentUsage");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Month).HasMaxLength(PlatformTenantShipmentUsage.MonthLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.ChargedAmount).HasPrecision(19, 4);
        builder.Property(item => item.CurrencyCode).HasMaxLength(PlatformSubscriptionLimits.CurrencyCodeLength).IsFixedLength().IsUnicode(false);
        builder.HasIndex(item => new { item.PlatformTenantId, item.Month }, "IX_TenantShipmentUsage_OnePerMonth").IsUnique();
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(item => item.IsCharged);
    }
}
