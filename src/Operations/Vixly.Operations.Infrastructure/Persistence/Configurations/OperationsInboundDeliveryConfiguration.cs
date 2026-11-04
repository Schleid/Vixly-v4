using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInboundDeliveryConfiguration.cs
//
// PURPOSE:
// Maps carrier deliveries to ops.InboundDeliveries (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns, the opening reference and the warehouse link
//
// IMPORTANT NOTES:
// - A row version: two devices closing one delivery issue one receipt
// - The opening reference is unique, so a double click opens one delivery
// =========================================================
internal sealed class OperationsInboundDeliveryConfiguration : IEntityTypeConfiguration<OperationsInboundDelivery>
{
    public void Configure(EntityTypeBuilder<OperationsInboundDelivery> builder)
    {
        builder.ToTable("InboundDeliveries");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Carrier).HasMaxLength(OperationsReceivingLimits.CarrierMaxLength);
        builder.Property(item => item.OpenKey).HasMaxLength(60).IsUnicode(false);
        builder.HasIndex(item => new { item.TenantId, item.OpenKey }).IsUnique().HasFilter("[OpenKey] IS NOT NULL");
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.Status });
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
