using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsWarehouseReceiptConfiguration.cs
//
// PURPOSE:
// Maps warehouse receipts to ops.WarehouseReceipts (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns, the unique number and the delivery link (one receipt per carrier delivery)
//
// IMPORTANT NOTES:
// - A receipt number is unique in a tenant
// =========================================================
internal sealed class OperationsWarehouseReceiptConfiguration : IEntityTypeConfiguration<OperationsWarehouseReceipt>
{
    public void Configure(EntityTypeBuilder<OperationsWarehouseReceipt> builder)
    {
        builder.ToTable("WarehouseReceipts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Number).HasMaxLength(OperationsReceivingLimits.NumberMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Carrier).HasMaxLength(OperationsReceivingLimits.CarrierMaxLength);
        builder.HasIndex(item => new { item.TenantId, item.Number }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.CreatedUtc });
        builder.HasIndex(item => item.DeliveryId).IsUnique();
        builder.HasOne<OperationsInboundDelivery>().WithMany().HasForeignKey(item => item.DeliveryId).OnDelete(DeleteBehavior.Restrict);
    }
}
