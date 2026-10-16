using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformProviderEventConfiguration.cs
//
// PURPOSE:
// The provider events (webhook inbox) table.
//
// RESPONSIBILITIES:
// - Map PlatformProviderEvent to its table, keys and indexes
//
// IMPORTANT NOTES:
// - The unique event id makes a second delivery of the same event fail to save
// =========================================================
internal sealed class PlatformProviderEventConfiguration : IEntityTypeConfiguration<PlatformProviderEvent>
{
    public void Configure(EntityTypeBuilder<PlatformProviderEvent> builder)
    {
        builder.ToTable("ProviderEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProviderEventId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.Provider, item.ProviderEventId }).IsUnique();
        builder.Property(item => item.EventType).HasMaxLength(PlatformPaymentsLimits.EventTypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ObjectId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false);
        builder.Property(item => item.Outcome).HasMaxLength(PlatformPaymentsLimits.OutcomeMaxLength).IsUnicode(false);
    }
}
