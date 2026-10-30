using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInvoiceConfiguration.cs
//
// PURPOSE:
// Maps OperationsInvoice to the database (Slice 5F step 1).
//
// RESPONSIBILITIES:
// - Columns, the parties as columns, the JSON lines and taxes, and indexes
//
// IMPORTANT NOTES:
// - Number unique per company; one invoice per shipment and per order
// - Totals are columns so lists filter on what is owed
// =========================================================
internal sealed class OperationsInvoiceConfiguration : IEntityTypeConfiguration<OperationsInvoice>
{
    public void Configure(EntityTypeBuilder<OperationsInvoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(invoice => invoice.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(invoice => invoice.Number).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(invoice => invoice.Reference).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(invoice => invoice.Route).HasMaxLength(300);
        builder.Property(invoice => invoice.ServiceCode).HasMaxLength(40).IsUnicode(false);
        builder.Property(invoice => invoice.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(invoice => invoice.Language).HasMaxLength(5).IsUnicode(false).IsRequired();
        builder.Property(invoice => invoice.TaxName).HasMaxLength(OperationsBillingLimits.TaxNameMaxLength);
        builder.Property(invoice => invoice.Note).HasMaxLength(OperationsBillingLimits.NoteMaxLength);

        foreach (string money in new[] { nameof(OperationsInvoice.Gross), nameof(OperationsInvoice.DiscountTotal), nameof(OperationsInvoice.Subtotal), nameof(OperationsInvoice.TaxTotal),
            nameof(OperationsInvoice.Total), nameof(OperationsInvoice.CreditedTotal), nameof(OperationsInvoice.PaidTotal), nameof(OperationsInvoice.PendingTotal),
            nameof(OperationsInvoice.RefundedTotal) })
        {
            builder.Property<decimal>(money).HasPrecision(14, 2);
        }

        builder.OwnsOne(invoice => invoice.Party, party =>
        {
            party.Property(item => item.CompanyName).HasColumnName("CompanyName").HasMaxLength(200).IsRequired();
            party.Property(item => item.TaxNumber).HasColumnName("TaxNumber").HasMaxLength(OperationsBillingLimits.TaxNumberMaxLength);
            party.Property(item => item.WarehouseName).HasColumnName("WarehouseName").HasMaxLength(200).IsRequired();
            party.Property(item => item.CustomerCode).HasColumnName("CustomerCode").HasMaxLength(20).IsUnicode(false);
            party.Property(item => item.CustomerName).HasColumnName("CustomerName").HasMaxLength(200).IsRequired();
            party.Property(item => item.CustomerCompany).HasColumnName("CustomerCompany").HasMaxLength(200);
            party.Property(item => item.CustomerPhone).HasColumnName("CustomerPhone").HasMaxLength(40).IsUnicode(false).IsRequired();
            party.Property(item => item.CustomerEmail).HasColumnName("CustomerEmail").HasMaxLength(320).IsRequired();
        });
        builder.Navigation(invoice => invoice.Party).IsRequired();
        builder.OwnsMany(invoice => invoice.Lines, line =>
        {
            line.ToJson("Lines");
            line.Property(item => item.Quantity).HasPrecision(12, 3);
            line.Property(item => item.Basis).HasPrecision(12, 3);
            line.Property(item => item.UnitPrice).HasPrecision(12, 4);
            line.Property(item => item.Gross).HasPrecision(14, 2);
            line.Property(item => item.DiscountPercent).HasPrecision(5, 2);
            line.Property(item => item.Discount).HasPrecision(14, 2);
            line.Property(item => item.Credited).HasPrecision(14, 2);
            line.Ignore(item => item.Amount);
            line.Ignore(item => item.Remaining);
        });
        builder.OwnsMany(invoice => invoice.Taxes, tax =>
        {
            tax.ToJson("Taxes");
            tax.Property(item => item.RatePercent).HasPrecision(6, 3);
            tax.Property(item => item.Basis).HasPrecision(14, 2);
            tax.Property(item => item.Amount).HasPrecision(14, 2);
            tax.Property(item => item.Credited).HasPrecision(14, 2);
        });
        builder.Navigation(invoice => invoice.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(invoice => invoice.Taxes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(invoice => invoice.Owed);
        builder.Ignore(invoice => invoice.CanVoid);
        builder.Ignore(invoice => invoice.Refundable);
        builder.Ignore(invoice => invoice.PaymentState);
        builder.Ignore(invoice => invoice.Released);
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(invoice => invoice.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(invoice => invoice.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(invoice => invoice.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsSupplySale>().WithMany().HasForeignKey(invoice => invoice.SupplySaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(invoice => new { invoice.TenantId, invoice.Number }).IsUnique();
        builder.HasIndex(invoice => new { invoice.TenantId, invoice.ShipmentId }).IsUnique().HasFilter("[ShipmentId] IS NOT NULL AND [Kind] = 1");
        builder.HasIndex(invoice => new { invoice.TenantId, invoice.SupplySaleId }).IsUnique().HasFilter("[SupplySaleId] IS NOT NULL AND [Kind] = 2");
        builder.HasIndex(invoice => new { invoice.TenantId, invoice.CustomerId, invoice.IssuedUtc });
        builder.HasIndex(invoice => new { invoice.TenantId, invoice.WarehouseId, invoice.IssuedUtc });

        // Slice 5F step 2b: the invoices on credit accounts, and the charges waiting for the credit department.
        builder.HasIndex(invoice => new { invoice.TenantId, invoice.OnAccount, invoice.CustomerId }).HasFilter("[OnAccount] <> 0");
    }
}
