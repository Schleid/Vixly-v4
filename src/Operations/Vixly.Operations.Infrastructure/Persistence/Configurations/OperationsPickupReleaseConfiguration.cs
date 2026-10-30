using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPickupReleaseConfiguration.cs
//
// PURPOSE:
// Maps releases at the pickup counter to ops.PickupReleases (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Who collected, the proof kept, who released, the request key
//
// IMPORTANT NOTES:
// - The ID's last characters only; the files' keys, never the files
// =========================================================
internal sealed class OperationsPickupReleaseConfiguration : IEntityTypeConfiguration<OperationsPickupRelease>
{
    public void Configure(EntityTypeBuilder<OperationsPickupRelease> builder)
    {
        builder.ToTable("PickupReleases");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.CollectorName).HasMaxLength(OperationsPickupRelease.NameMaxLength).IsRequired();
        builder.Property(row => row.IdLast).HasMaxLength(OperationsPickupRelease.IdLastLength).IsUnicode(false);
        builder.Property(row => row.SignatureKey).HasMaxLength(200).IsUnicode(false);
        builder.Property(row => row.PhotoKey).HasMaxLength(200).IsUnicode(false);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(row => row.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(row => row.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.ShipmentId });
        builder.HasIndex(row => new { row.TenantId, row.RequestKey }).IsUnique().HasFilter("[RequestKey] IS NOT NULL");
    }
}
