using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformInvoiceConfiguration.cs
//
// PURPOSE:
// Maps invoices with their lines, payments and credit notes (Slice 4).
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - Row version protects the balance
// - Source key unique: one invoice per subscription event (D-B3)
// - Number unique across the platform (D-B2)
// =========================================================
internal sealed class PlatformInvoiceConfiguration : IEntityTypeConfiguration<PlatformInvoice>
{
    public void Configure(EntityTypeBuilder<PlatformInvoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Number).HasMaxLength(PlatformBillingLimits.NumberMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Number).IsUnique();
        builder.Property(item => item.SourceKey).HasMaxLength(PlatformBillingLimits.SourceKeyMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.SourceKey).IsUnique();
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.Language).HasConversion(PlatformValueConverters.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
        foreach (string money in new[] { nameof(PlatformInvoice.NetTotal), nameof(PlatformInvoice.DiscountTotal), nameof(PlatformInvoice.TaxTotal), nameof(PlatformInvoice.Total), nameof(PlatformInvoice.PaidTotal), nameof(PlatformInvoice.CreditedTotal), nameof(PlatformInvoice.RefundedTotal), nameof(PlatformInvoice.Balance) })
        {
            builder.Property(money).HasPrecision(18, 2);
        }

        builder.HasIndex(item => new { item.PlatformTenantId, item.IssuedUtc });
        builder.HasIndex(item => new { item.Status, item.DueUtc });
        builder.HasIndex(item => new { item.PlatformSubscriptionId, item.Status });
        builder.HasOne<Vixly.Platform.Domain.Tenants.PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Platform.Domain.Subscriptions.PlatformSubscription>().WithMany().HasForeignKey(item => item.PlatformSubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(item => item.PendingCredit);
        builder.Ignore(item => item.RefundOwed);
        builder.Ignore(item => item.IsPartlyPaid);

        builder.ComplexProperty(item => item.Customer, customer =>
        {
            customer.Property(value => value.Name).HasColumnName("CustomerName").HasMaxLength(PlatformBillingLimits.NameMaxLength).IsRequired();
            customer.Property(value => value.Address).HasColumnName("CustomerAddress").HasMaxLength(PlatformBillingLimits.AddressMaxLength).IsRequired();
            customer.Property(value => value.TaxId).HasColumnName("CustomerTaxId").HasMaxLength(PlatformBillingLimits.TaxIdMaxLength);
            customer.Property(value => value.Email).HasColumnName("CustomerEmail").HasMaxLength(PlatformBillingLimits.EmailMaxLength).IsRequired();
        });

        builder.OwnsMany(item => item.Lines, line =>
        {
            line.ToTable("InvoiceLines");
            line.WithOwner().HasForeignKey(value => value.PlatformInvoiceId);
            line.HasKey(value => value.Id);
            line.Property(value => value.Id).ValueGeneratedNever();
            line.Property(value => value.TextKey).HasMaxLength(PlatformBillingLimits.TextKeyMaxLength).IsUnicode(false).IsRequired();
            line.Property(value => value.Arguments).HasConversion(PlatformValueConverters.Arguments, PlatformValueConverters.ArgumentsComparer).IsRequired();
            line.Property(value => value.Quantity).HasPrecision(18, 2);
            line.Property(value => value.UnitPrice).HasPrecision(18, 2);
            line.Property(value => value.Net).HasPrecision(18, 2);
            line.Property(value => value.TaxRate).HasPrecision(9, 4);
            line.Property(value => value.Tax).HasPrecision(18, 2);
            line.Property(value => value.TaxLabel).HasMaxLength(PlatformBillingLimits.TaxLabelMaxLength);
        });
        builder.Navigation(item => item.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(item => item.Payments, payment =>
        {
            payment.ToTable("Payments");
            payment.WithOwner().HasForeignKey(value => value.PlatformInvoiceId);
            payment.HasKey(value => value.Id);
            payment.Property(value => value.Id).ValueGeneratedNever();
            payment.Property(value => value.ReceiptNumber).HasMaxLength(PlatformBillingLimits.NumberMaxLength).IsUnicode(false).IsRequired();
            payment.HasIndex(value => value.ReceiptNumber).IsUnique();
            payment.Property(value => value.Amount).HasPrecision(18, 2);
            payment.Property(value => value.Reference).HasMaxLength(PlatformBillingLimits.ReferenceMaxLength);
            payment.Property(value => value.Note).HasMaxLength(PlatformBillingLimits.NoteMaxLength);
            payment.Property(value => value.ReversalReason).HasMaxLength(PlatformBillingLimits.ReasonMaxLength);
            payment.HasIndex(value => value.ReceivedUtc);
            payment.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(value => value.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
            payment.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(value => value.ReversedByUserId).OnDelete(DeleteBehavior.Restrict);
            payment.Ignore(value => value.Counts);
        });
        builder.Navigation(item => item.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(item => item.Credits, credit =>
        {
            credit.ToTable("CreditNotes");
            credit.WithOwner().HasForeignKey(value => value.PlatformInvoiceId);
            credit.HasKey(value => value.Id);
            credit.Property(value => value.Id).ValueGeneratedNever();
            credit.Property(value => value.Number).HasMaxLength(PlatformBillingLimits.NumberMaxLength).IsUnicode(false);
            credit.HasIndex(value => value.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
            credit.HasIndex(value => value.PlatformInvoiceId, "IX_CreditNotes_OnePending").IsUnique().HasFilter("[Status] = 0");
            credit.Property(value => value.Amount).HasPrecision(18, 2);
            credit.Property(value => value.TaxPart).HasPrecision(18, 2);
            credit.Property(value => value.Reason).HasMaxLength(PlatformBillingLimits.ReasonMaxLength).IsRequired();
            credit.Property(value => value.DecisionReason).HasMaxLength(PlatformBillingLimits.ReasonMaxLength);
            credit.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(value => value.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            credit.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(value => value.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
            credit.Ignore(value => value.IsPending);
            credit.Ignore(value => value.IsIssued);
        });
        builder.Navigation(item => item.Credits).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
