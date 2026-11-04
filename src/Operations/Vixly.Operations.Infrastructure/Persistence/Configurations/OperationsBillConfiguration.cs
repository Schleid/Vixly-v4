using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsBillConfiguration.cs
//
// PURPOSE:
// Maps a bill the company received, its shares on dispatches and shipments, and its payments (Slice 8).
//
// RESPONSIBILITIES:
// - Columns and indexes; the shares in their own table (summed by dispatch and by shipment), the payments as JSON
//
// IMPORTANT NOTES:
// - Money as decimal(18,2)
// =========================================================
internal sealed class OperationsBillConfiguration : IEntityTypeConfiguration<OperationsBill>
{
    public void Configure(EntityTypeBuilder<OperationsBill> builder)
    {
        builder.ToTable("Bills");
        builder.HasKey(bill => bill.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(bill => bill.PartyName).HasMaxLength(OperationsVendor.NameMaxLength).IsRequired();
        builder.Property(bill => bill.Reference).HasMaxLength(OperationsBill.ReferenceMaxLength).IsRequired();
        builder.Property(bill => bill.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(bill => bill.Note).HasMaxLength(OperationsBill.NoteMaxLength);
        builder.Property(bill => bill.RejectReason).HasMaxLength(OperationsBill.NoteMaxLength);
        builder.Property(bill => bill.VoidReason).HasMaxLength(OperationsBill.NoteMaxLength);
        builder.Ignore(bill => bill.Editors);
        builder.PrimitiveCollection<List<Guid>>("_editors").HasColumnName("Editors");
        builder.Property(bill => bill.Amount).HasPrecision(18, 2);
        builder.Property(bill => bill.PaidTotal).HasPrecision(18, 2);
        builder.Ignore(bill => bill.Allocated);
        builder.Ignore(bill => bill.Owed);
        builder.Ignore(bill => bill.Counts);
        builder.OwnsMany(bill => bill.Allocations, allocation =>
        {
            allocation.ToTable("BillAllocations");
            allocation.WithOwner().HasForeignKey("BillId");
            allocation.HasKey(item => item.Id);
            allocation.Property(item => item.Id).ValueGeneratedNever();
            allocation.Property(item => item.Reference).HasMaxLength(40).IsRequired();
            allocation.Property(item => item.Amount).HasPrecision(18, 2);
            allocation.HasIndex(item => item.DispatchId);
            allocation.HasIndex(item => item.ShipmentId);
        });
        builder.OwnsMany(bill => bill.Payments, payment =>
        {
            payment.ToJson("Payments");
            payment.Property(item => item.Amount).HasPrecision(18, 2);
        });
        builder.Navigation(bill => bill.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(bill => bill.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(bill => new { bill.TenantId, bill.Status, bill.DueOn });
        builder.HasIndex(bill => new { bill.TenantId, bill.Party, bill.Reference });
    }
}
