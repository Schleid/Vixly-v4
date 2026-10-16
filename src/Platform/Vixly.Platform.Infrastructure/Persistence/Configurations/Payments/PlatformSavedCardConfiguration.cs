using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformSavedCardConfiguration.cs
//
// PURPOSE:
// The saved cards table.
//
// RESPONSIBILITIES:
// - Map PlatformSavedCard to its table, keys and indexes
//
// IMPORTANT NOTES:
// - Only reference, brand, last four and expiry (PCI SAQ A)
// =========================================================
internal sealed class PlatformSavedCardConfiguration : IEntityTypeConfiguration<PlatformSavedCard>
{
    public void Configure(EntityTypeBuilder<PlatformSavedCard> builder)
    {
        builder.ToTable("SavedCards");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProviderPaymentMethodId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.ProviderPaymentMethodId).IsUnique();
        builder.Property(item => item.Brand).HasMaxLength(PlatformPaymentsLimits.BrandMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Last4).HasMaxLength(4).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.PlatformTenantId, item.Status });
        builder.Ignore(item => item.IsActive);
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.RemovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
