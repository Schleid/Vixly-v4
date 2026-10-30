using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsStockCountConfiguration.cs
//
// PURPOSE:
// Maps the counts of the undeclared parcels area to ops.StockCounts (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - The lines as JSON inside the count; one open count per warehouse
//
// IMPORTANT NOTES:
// - A row version, so two clerks marking at once never overwrite each other
// =========================================================
internal sealed class OperationsStockCountConfiguration : IEntityTypeConfiguration<OperationsStockCount>
{
    public void Configure(EntityTypeBuilder<OperationsStockCount> builder)
    {
        builder.ToTable("StockCounts");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.IsOpen);
        builder.OwnsMany(item => item.Lines, line =>
        {
            line.ToJson("Lines");
            line.Property(item => item.ParcelNumber).HasMaxLength(Vixly.Operations.Domain.Receiving.OperationsReceivingLimits.NumberMaxLength);
            line.Property(item => item.Note).HasMaxLength(OperationsStockCount.NoteMaxLength);
        });
        builder.Navigation(item => item.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.CreatedUtc });
        builder.HasIndex(item => new { item.TenantId, item.OpenWarehouseId }).IsUnique().HasFilter("[OpenWarehouseId] IS NOT NULL");
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
