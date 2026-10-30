using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsRefundConfiguration.cs
//
// PURPOSE:
// Maps OperationsRefund to the database (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - Number and form key unique per company
// =========================================================
internal sealed class OperationsRefundConfiguration : IEntityTypeConfiguration<OperationsRefund>
{
    public void Configure(EntityTypeBuilder<OperationsRefund> builder)
    {
        builder.ToTable("Refunds");
        builder.HasKey(refund => refund.Id);
        builder.Property(refund => refund.Number).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(refund => refund.Amount).HasPrecision(14, 2);
        builder.Property(refund => refund.Reason).HasMaxLength(OperationsBillingLimits.ReasonMaxLength).IsRequired();
        builder.Property(refund => refund.Reference).HasMaxLength(60);
        builder.HasOne<OperationsInvoice>().WithMany().HasForeignKey(refund => refund.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsCashDrawer>().WithMany().HasForeignKey(refund => refund.DrawerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(refund => new { refund.TenantId, refund.Number }).IsUnique();
        builder.HasIndex(refund => new { refund.TenantId, refund.RequestKey }).IsUnique();
        builder.HasIndex(refund => new { refund.TenantId, refund.InvoiceId });
    }
}
