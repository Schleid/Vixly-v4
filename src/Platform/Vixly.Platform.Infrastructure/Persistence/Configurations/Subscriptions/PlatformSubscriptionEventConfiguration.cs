using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionEventConfiguration.cs
//
// PURPOSE:
// Maps the subscription history.
//
// RESPONSIBILITIES:
// - Map SubscriptionEvents, append-only
//
// IMPORTANT NOTES:
// - The app login has no UPDATE or DELETE on this table (global SQL script)
// =========================================================
internal sealed class PlatformSubscriptionEventConfiguration : IEntityTypeConfiguration<PlatformSubscriptionEvent>
{
    public void Configure(EntityTypeBuilder<PlatformSubscriptionEvent> builder)
    {
        builder.ToTable("SubscriptionEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventCode).HasMaxLength(PlatformSubscriptionLimits.EventCodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(PlatformSubscriptionLimits.ReasonMaxLength);
        builder.Property(item => item.Arguments)
            .HasConversion(PlatformValueConverters.Arguments, PlatformValueConverters.ArgumentsComparer)
            .IsRequired();
        builder.HasOne<PlatformSubscription>().WithMany().HasForeignKey(item => item.PlatformSubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PlatformTenantId, item.CreatedUtc });
        builder.HasIndex(item => new { item.PlatformSubscriptionId, item.CreatedUtc });
    }
}
