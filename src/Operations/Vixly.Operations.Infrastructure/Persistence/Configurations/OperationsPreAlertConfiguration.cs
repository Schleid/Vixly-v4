using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPreAlertConfiguration.cs
//
// PURPOSE:
// Maps customer pre-alerts to ops.PreAlerts (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns, the one-open-per-customer-and-number rule and the customer link
//
// IMPORTANT NOTES:
// - One open pre-alert per customer and tracking number: the database refuses a second; another customer's pre-alert never blocks (review H1)
// - A row version: two clerks cannot both match one pre-alert (review H2)
// =========================================================
internal sealed class OperationsPreAlertConfiguration : IEntityTypeConfiguration<OperationsPreAlert>
{
    public void Configure(EntityTypeBuilder<OperationsPreAlert> builder)
    {
        builder.ToTable("PreAlerts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Carrier).HasMaxLength(OperationsReceivingLimits.CarrierMaxLength).IsRequired();
        builder.Property(item => item.TrackingNumber).HasMaxLength(OperationsReceivingLimits.TrackingNumberMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Store).HasMaxLength(OperationsReceivingLimits.StoreMaxLength);
        builder.Property(item => item.Contents).HasMaxLength(OperationsReceivingLimits.ContentsMaxLength).IsRequired();
        builder.Property(item => item.DeclaredValue).HasPrecision(12, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.TrackingNumber }).IsUnique().HasFilter("[Status] = 0");
        builder.HasIndex(item => new { item.TenantId, item.TrackingNumber, item.Status });
        builder.HasIndex(item => new { item.TenantId, item.CustomerId });
        builder.HasIndex(item => new { item.TenantId, item.ShipmentId });

        // Slice 4D: the warehouse it is shipped to; the incoming list reads it by warehouse, status and expected day.
        builder.HasOne<Vixly.Operations.Domain.Locations.OperationsWarehouse>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.TenantId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.Status, item.ExpectedDate });
        builder.HasOne<OperationsShipment>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.ShipmentId })
            .HasPrincipalKey(shipment => new { shipment.TenantId, shipment.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsCustomer>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.CustomerId })
            .HasPrincipalKey(customer => new { customer.TenantId, customer.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
