using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPaymentConfiguration.cs
//
// PURPOSE:
// Maps OperationsPayment to the database (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Columns, the JSON tenders and indexes
//
// IMPORTANT NOTES:
// - Number and form key unique per company; Pending is a column for the list to verify
// =========================================================
internal sealed class OperationsPaymentConfiguration : IEntityTypeConfiguration<OperationsPayment>
{
    public void Configure(EntityTypeBuilder<OperationsPayment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(payment => payment.Number).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(payment => payment.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(payment => payment.Total).HasPrecision(14, 2);
        builder.Ignore(payment => payment.Change);
        builder.Ignore(payment => payment.Cash);
        builder.OwnsMany(payment => payment.Tenders, tender =>
        {
            tender.ToJson("Tenders");
            tender.Property(item => item.Amount).HasPrecision(14, 2);
            tender.Property(item => item.CashGiven).HasPrecision(14, 2);
            tender.Property(item => item.Change).HasPrecision(14, 2);
        });
        builder.Navigation(payment => payment.Tenders).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<OperationsInvoice>().WithMany().HasForeignKey(payment => payment.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(payment => payment.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(payment => payment.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsCashDrawer>().WithMany().HasForeignKey(payment => payment.DrawerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => new { payment.TenantId, payment.Number }).IsUnique();
        builder.HasIndex(payment => new { payment.TenantId, payment.RequestKey }).IsUnique();
        builder.HasIndex(payment => new { payment.TenantId, payment.InvoiceId });
        builder.HasIndex(payment => new { payment.TenantId, payment.WarehouseId, payment.Pending });
    }
}
