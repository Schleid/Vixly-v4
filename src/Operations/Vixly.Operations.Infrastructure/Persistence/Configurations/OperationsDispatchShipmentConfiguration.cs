using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDispatchShipmentConfiguration.cs
//
// PURPOSE:
// Maps the shipments on a dispatch to ops.DispatchShipments (Slice 6B step 2).
//
// RESPONSIBILITIES:
// - House number, pieces, weights, value, cost share
//
// IMPORTANT NOTES:
// - One per shipment per dispatch; a house number is unique in the company
// =========================================================
internal sealed class OperationsDispatchShipmentConfiguration : IEntityTypeConfiguration<OperationsDispatchShipment>
{
    public void Configure(EntityTypeBuilder<OperationsDispatchShipment> builder)
    {
        builder.ToTable("DispatchShipments");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.HouseNumber).HasMaxLength(48).IsUnicode(false).IsRequired();
        builder.Property(row => row.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(row => row.WeightKg).HasPrecision(12, 3);
        builder.Property(row => row.ChargeableKg).HasPrecision(12, 3);
        builder.Property(row => row.DeclaredValue).HasPrecision(14, 2);
        builder.Property(row => row.CostShare).HasPrecision(14, 2);
        builder.HasOne<OperationsDispatch>().WithMany().HasForeignKey(row => row.DispatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(row => row.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.DispatchId, row.ShipmentId }).IsUnique();
        builder.HasIndex(row => new { row.TenantId, row.HouseNumber }).IsUnique();
        builder.HasIndex(row => new { row.TenantId, row.ShipmentId });
    }
}
