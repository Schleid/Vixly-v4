using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsParcelConfiguration.cs
//
// PURPOSE:
// Maps parcels to ops.Parcels (Slice 3A; receiving columns Slice 3B).
//
// RESPONSIBILITIES:
// - Keep the projected milestone, flags and holds
// - Find a customer's parcels, a warehouse's parcels by carrier tracking number
//
// IMPORTANT NOTES:
// - The parcel number is unique in a tenant; a parcel links to its shipment, warehouse, shelf and pre-alert
// =========================================================
internal sealed class OperationsParcelConfiguration : IEntityTypeConfiguration<OperationsParcel>
{
    public void Configure(EntityTypeBuilder<OperationsParcel> builder)
    {
        builder.ToTable("Parcels");
        builder.HasKey(item => item.Id);

        // Two writers on one parcel cannot overwrite each other's projection (review H1).
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => new { item.TenantId, item.CustomerId });
        builder.HasIndex(item => new { item.TenantId, item.Milestone });

        // Slice 3B: receiving.
        builder.Property(item => item.Number).HasMaxLength(OperationsReceivingLimits.NumberMaxLength).IsUnicode(false);
        builder.Property(item => item.Carrier).HasMaxLength(OperationsReceivingLimits.CarrierMaxLength);
        builder.Property(item => item.CarrierTrackingNumber).HasMaxLength(OperationsReceivingLimits.TrackingNumberMaxLength).IsUnicode(false);
        builder.Property(item => item.WeightKg).HasPrecision(9, 3);
        builder.Property(item => item.LengthCm).HasPrecision(7, 1);
        builder.Property(item => item.WidthCm).HasPrecision(7, 1);
        builder.Property(item => item.HeightCm).HasPrecision(7, 1);
        builder.Property(item => item.DeclaredContents).HasMaxLength(OperationsReceivingLimits.ContentsMaxLength);
        builder.Property(item => item.DeclaredValue).HasPrecision(12, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false);
        builder.Ignore(item => item.State);
        builder.Ignore(item => item.IsIdentified);
        builder.HasIndex(item => new { item.TenantId, item.Number }).IsUnique().HasFilter("[Number] IS NOT NULL");
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.CarrierTrackingNumber });
        builder.Property(item => item.OriginKey).HasMaxLength(80).IsUnicode(false);
        builder.Ignore(item => item.IsAtOrigin);

        // One parcel per carrier tracking number at a warehouse while it is there: two clerks scanning it at once get one parcel (review H2).
        builder.HasIndex(item => new { item.TenantId, item.OriginKey }).IsUnique().HasFilter("[OriginKey] IS NOT NULL");
        builder.HasIndex(item => new { item.TenantId, item.ShipmentId });

        // Slice 4D step 3: what the label of an undeclared package shows, searched in the undeclared parcels.
        builder.Property(item => item.LabelName).HasMaxLength(100);
        builder.Property(item => item.LabelCode).HasMaxLength(20).IsUnicode(false);
        builder.Property(item => item.LabelSender).HasMaxLength(100);
        builder.Property(item => item.StorageFeePerDay).HasPrecision(12, 2);

        // Slice 7 step 2: the destination's storage terms.
        builder.Property(item => item.DestinationFeePerDay).HasPrecision(12, 2);
        builder.Ignore(item => item.OwesStorage);

        // Slice 4D step 2: the labels to print of a warehouse.
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.ShippingLabelUtc });
        builder.HasIndex(item => new { item.TenantId, item.DeliveryId });
        builder.HasOne<OperationsInboundDelivery>().WithMany().HasForeignKey(item => item.DeliveryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(item => item.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsStorageLocation>().WithMany().HasForeignKey(item => item.StorageLocationId).OnDelete(DeleteBehavior.Restrict);

        // Slice 6A: the bundle and the repack box it is in.
        builder.HasOne<Vixly.Operations.Domain.Bundles.OperationsBundle>().WithMany().HasForeignKey(item => item.BundleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Operations.Domain.Bundles.OperationsRepackBox>().WithMany().HasForeignKey(item => item.BoxId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TenantId, item.BundleId });
        builder.HasIndex(item => new { item.TenantId, item.BoxId });
        builder.HasOne<OperationsPreAlert>().WithMany().HasForeignKey(item => item.PreAlertId).OnDelete(DeleteBehavior.Restrict);

        // The customer must be of the parcel's own tenant: the key includes the tenant (review M2).
        builder.HasOne<OperationsCustomer>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.CustomerId })
            .HasPrincipalKey(customer => new { customer.TenantId, customer.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
