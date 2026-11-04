using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplyMovementConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupplyMovement to the database (Slice 5E step 2).
//
// RESPONSIBILITIES:
// - Columns and indexes of the stock movements
//
// IMPORTANT NOTES:
// - Append only; read newest first per supply and warehouse; by link for a form sent twice
// =========================================================
internal sealed class OperationsSupplyMovementConfiguration : IEntityTypeConfiguration<OperationsSupplyMovement>
{
    public void Configure(EntityTypeBuilder<OperationsSupplyMovement> builder)
    {
        builder.ToTable("SupplyMovements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.UnitCost).HasPrecision(14, 4);
        builder.Property(movement => movement.Note).HasMaxLength(OperationsSupplyStockLimits.NoteMaxLength);
        builder.Property(movement => movement.Supplier).HasMaxLength(OperationsSupplyStockLimits.SupplierMaxLength);
        builder.Property(movement => movement.Reference).HasMaxLength(OperationsSupplyStockLimits.ReferenceMaxLength);
        builder.HasOne<OperationsSupply>().WithMany().HasForeignKey(movement => movement.SupplyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(movement => movement.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(movement => new { movement.TenantId, movement.SupplyId, movement.WarehouseId, movement.CreatedUtc });
        builder.HasIndex(movement => new { movement.TenantId, movement.LinkId });
    }
}
