using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDeliveryStopConfiguration.cs
//
// PURPOSE:
// Maps the stops of delivery routes to ops.DeliveryStops (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Route, shipment, order, how it went, the proof kept, the cash collected; a row version
//
// IMPORTANT NOTES:
// - The ID's last characters only; the files' keys, never the files
// - A shipment is once on a route
// =========================================================
internal sealed class OperationsDeliveryStopConfiguration : IEntityTypeConfiguration<OperationsDeliveryStop>
{
    public void Configure(EntityTypeBuilder<OperationsDeliveryStop> builder)
    {
        builder.ToTable("DeliveryStops");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.ReceiverName).HasMaxLength(OperationsPickupRelease.NameMaxLength);
        builder.Property(row => row.IdLast).HasMaxLength(OperationsPickupRelease.IdLastLength).IsUnicode(false);
        builder.Property(row => row.SignatureKey).HasMaxLength(200).IsUnicode(false);
        builder.Property(row => row.PhotoKey).HasMaxLength(200).IsUnicode(false);
        builder.Property(row => row.CashCollected).HasPrecision(14, 2);
        builder.Property(row => row.FailureReason).HasMaxLength(40).IsUnicode(false);
        builder.Property(row => row.Note).HasMaxLength(OperationsDeliveryStop.NoteMaxLength);
        builder.Ignore(row => row.IsDone);
        builder.Ignore(row => row.CollectedInvoiceIds);
        builder.Property(row => row.CollectedInvoices).HasMaxLength(400).IsUnicode(false);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne<OperationsDeliveryRoute>().WithMany().HasForeignKey(row => row.RouteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(row => row.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.RouteId, row.ShipmentId }).IsUnique();
        builder.HasIndex(row => new { row.TenantId, row.ShipmentId });
    }
}
