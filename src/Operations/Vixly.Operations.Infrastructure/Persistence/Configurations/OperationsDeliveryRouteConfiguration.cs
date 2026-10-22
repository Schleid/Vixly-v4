using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDeliveryRouteConfiguration.cs
//
// PURPOSE:
// Maps delivery routes to ops.DeliveryRoutes (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Number, warehouse, day, driver, status, cash, the request key; a row version
//
// IMPORTANT NOTES:
// - The driver and the stops change it at once: the row version refuses the later save
// =========================================================
internal sealed class OperationsDeliveryRouteConfiguration : IEntityTypeConfiguration<OperationsDeliveryRoute>
{
    public void Configure(EntityTypeBuilder<OperationsDeliveryRoute> builder)
    {
        builder.ToTable("DeliveryRoutes");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Number).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(row => row.DriverName).HasMaxLength(OperationsDeliveryRoute.NameMaxLength).IsRequired();
        builder.Property(row => row.CashCollected).HasPrecision(14, 2);
        builder.Property(row => row.CashHandedIn).HasPrecision(14, 2);
        builder.Property(row => row.SettleNote).HasMaxLength(OperationsDeliveryRoute.NoteMaxLength);
        builder.Property(row => row.CancelReason).HasMaxLength(OperationsDeliveryRoute.NoteMaxLength);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(row => row.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.Number }).IsUnique();
        builder.HasIndex(row => new { row.TenantId, row.WarehouseId, row.ServiceDate });
        builder.HasIndex(row => new { row.TenantId, row.DriverUserId, row.ServiceDate }).HasFilter("[DriverUserId] IS NOT NULL");
        builder.HasIndex(row => new { row.TenantId, row.RequestKey }).IsUnique().HasFilter("[RequestKey] IS NOT NULL");
    }
}
