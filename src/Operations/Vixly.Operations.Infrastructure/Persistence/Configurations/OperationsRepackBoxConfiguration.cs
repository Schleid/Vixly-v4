using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsRepackBoxConfiguration.cs
//
// PURPOSE:
// Maps OperationsRepackBox to the database (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One box not voided per shipment (unique filtered index)
// =========================================================
internal sealed class OperationsRepackBoxConfiguration : IEntityTypeConfiguration<OperationsRepackBox>
{
    public void Configure(EntityTypeBuilder<OperationsRepackBox> builder)
    {
        builder.ToTable("RepackBoxes");
        builder.HasKey(box => box.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(box => box.Number).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(box => box.VoidReason).HasMaxLength(OperationsBundleLimits.ReasonMaxLength);
        builder.Property(box => box.ParcelsWeightKg).HasPrecision(12, 3);
        builder.Property(box => box.WeightKg).HasPrecision(12, 3);

        foreach (string side in new[] { nameof(OperationsRepackBox.LengthCm), nameof(OperationsRepackBox.WidthCm), nameof(OperationsRepackBox.HeightCm) })
        {
            builder.Property<decimal?>(side).HasPrecision(8, 1);
        }

        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(box => box.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(box => box.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsBundle>().WithMany().HasForeignKey(box => box.BundleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(box => new { box.TenantId, box.Number }).IsUnique();
        builder.HasIndex(box => new { box.TenantId, box.ShipmentId }).IsUnique().HasFilter("[Status] <> 3");
        builder.HasIndex(box => new { box.TenantId, box.WarehouseId, box.Status });
        builder.HasIndex(box => new { box.TenantId, box.BundleId });
    }
}
