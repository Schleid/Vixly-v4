using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplyStockConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupplyStock to the database (Slice 5E step 2).
//
// RESPONSIBILITIES:
// - Columns, the row version and the unique index
//
// IMPORTANT NOTES:
// - One row per supply and warehouse
// =========================================================
internal sealed class OperationsSupplyStockConfiguration : IEntityTypeConfiguration<OperationsSupplyStock>
{
    public void Configure(EntityTypeBuilder<OperationsSupplyStock> builder)
    {
        builder.ToTable("SupplyStocks");
        builder.HasKey(stock => stock.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(stock => stock.IsLow);
        builder.HasOne<OperationsSupply>().WithMany().HasForeignKey(stock => stock.SupplyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(stock => stock.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(stock => new { stock.TenantId, stock.SupplyId, stock.WarehouseId }).IsUnique();
        builder.HasIndex(stock => new { stock.TenantId, stock.WarehouseId });
    }
}
