using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformPaymentLinkConfiguration.cs
//
// PURPOSE:
// The payment links table.
//
// RESPONSIBILITIES:
// - Map PlatformPaymentLink to its table, keys and indexes
//
// IMPORTANT NOTES:
// - Only the SHA-256 of the token is stored
// =========================================================
internal sealed class PlatformPaymentLinkConfiguration : IEntityTypeConfiguration<PlatformPaymentLink>
{
    public void Configure(EntityTypeBuilder<PlatformPaymentLink> builder)
    {
        builder.ToTable("PaymentLinks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash).HasMaxLength(PlatformPaymentsLimits.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.PlatformTenantId, item.Kind });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformInvoice>().WithMany().HasForeignKey(item => item.PlatformInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
