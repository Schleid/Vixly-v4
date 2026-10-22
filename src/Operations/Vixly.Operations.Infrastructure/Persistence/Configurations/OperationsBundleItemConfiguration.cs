using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsBundleItemConfiguration.cs
//
// PURPOSE:
// Maps a bundle's list to ops.BundleItems (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - The bundle, the parcel or box, its shipment, who listed it, who scanned it
//
// IMPORTANT NOTES:
// - Once per bundle per parcel or box
// =========================================================
internal sealed class OperationsBundleItemConfiguration : IEntityTypeConfiguration<OperationsBundleItem>
{
    public void Configure(EntityTypeBuilder<OperationsBundleItem> builder)
    {
        builder.ToTable("BundleItems");
        builder.HasKey(row => row.Id);
        builder.Ignore(row => row.IsScanned);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne<OperationsBundle>().WithMany().HasForeignKey(row => row.BundleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsParcel>().WithMany().HasForeignKey(row => row.ParcelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsRepackBox>().WithMany().HasForeignKey(row => row.BoxId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(row => row.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.BundleId, row.ParcelId }).IsUnique().HasFilter("[ParcelId] IS NOT NULL");
        builder.HasIndex(row => new { row.TenantId, row.BundleId, row.BoxId }).IsUnique().HasFilter("[BoxId] IS NOT NULL");
        // On one list at a time (review): a voided bundle's lines are removed.
        builder.HasIndex(row => new { row.TenantId, row.ParcelId }).IsUnique().HasFilter("[ParcelId] IS NOT NULL");
        builder.HasIndex(row => new { row.TenantId, row.BoxId }).IsUnique().HasFilter("[BoxId] IS NOT NULL");
    }
}
