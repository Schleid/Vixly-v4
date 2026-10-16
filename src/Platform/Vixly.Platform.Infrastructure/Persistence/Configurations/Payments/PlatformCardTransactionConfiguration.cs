using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Payments;

// =========================================================
// FILE: PlatformCardTransactionConfiguration.cs
//
// PURPOSE:
// The card transactions table.
//
// RESPONSIBILITIES:
// - Map PlatformCardTransaction to its table, keys and indexes
//
// IMPORTANT NOTES:
// - The filtered unique index enforces one open charge per invoice across hosts
// - Status values: 1 Created, 2 Pending, 3 Requires action
// =========================================================
internal sealed class PlatformCardTransactionConfiguration : IEntityTypeConfiguration<PlatformCardTransaction>
{
    public void Configure(EntityTypeBuilder<PlatformCardTransaction> builder)
    {
        builder.ToTable("CardTransactions");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.IdempotencyKey).HasMaxLength(60).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.Property(item => item.ProviderCheckoutId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false);
        builder.Property(item => item.ProviderPaymentId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false);
        builder.Property(item => item.ProviderRefundId).HasMaxLength(PlatformPaymentsLimits.ProviderIdMaxLength).IsUnicode(false);
        builder.Property(item => item.FailureCode).HasMaxLength(PlatformPaymentsLimits.FailureCodeMaxLength).IsUnicode(false);
        builder.HasIndex(item => item.ProviderCheckoutId).HasFilter("[ProviderCheckoutId] IS NOT NULL");
        builder.HasIndex(item => item.ProviderPaymentId).HasFilter("[ProviderPaymentId] IS NOT NULL");
        builder.HasIndex(item => item.ProviderRefundId).HasFilter("[ProviderRefundId] IS NOT NULL");
        builder.HasIndex(item => new { item.PlatformInvoiceId, item.CreatedUtc });
        builder.HasIndex(item => new { item.PlatformTenantId, item.CreatedUtc });
        builder.HasIndex(item => new { item.Status, item.LastModifiedUtc });

        // At most one charge per invoice is with the provider at a time (D-P5): a second one fails to save.
        builder.HasIndex(item => item.PlatformInvoiceId)
            .IsUnique()
            .HasFilter("[Kind] = 1 AND [Status] IN (1, 2, 3)")
            .HasDatabaseName("IX_CardTransactions_OneOpenChargePerInvoice");

        builder.Ignore(item => item.IsOpen);
        builder.Ignore(item => item.IsFinal);
        builder.Ignore(item => item.MinorUnits);
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformInvoice>().WithMany().HasForeignKey(item => item.PlatformInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformSavedCard>().WithMany().HasForeignKey(item => item.SavedCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformCardTransaction>().WithMany().HasForeignKey(item => item.ChargeTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
