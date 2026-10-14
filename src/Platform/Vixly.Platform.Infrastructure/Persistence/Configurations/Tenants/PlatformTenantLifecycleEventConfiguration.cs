using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformTenantLifecycleEventConfiguration.cs
//
// PURPOSE:
// Maps tenant history to platform.TenantLifecycleEvents.
//
// RESPONSIBILITIES:
// - Event code, statuses, category, reason and actor as columns
// - Arguments as a JSON object
//
// IMPORTANT NOTES:
// - Append-only: the application never updates or deletes a row
// - Read newest first per tenant, hence the index
// =========================================================
internal sealed class PlatformTenantLifecycleEventConfiguration : IEntityTypeConfiguration<PlatformTenantLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<PlatformTenantLifecycleEvent> builder)
    {
        builder.ToTable("TenantLifecycleEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventCode).HasMaxLength(PlatformTenantLimits.EventCodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(PlatformTenantLimits.ReasonMaxLength);
        builder.Property(item => item.Arguments)
            .HasConversion(PlatformValueConverters.Arguments, PlatformValueConverters.ArgumentsComparer)
            .IsRequired();
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PlatformTenantId, item.CreatedUtc });
    }
}
