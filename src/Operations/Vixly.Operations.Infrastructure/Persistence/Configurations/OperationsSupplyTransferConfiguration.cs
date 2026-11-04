using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplyTransferConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupplyTransfer to the database (Slice 5E step 2).
//
// RESPONSIBILITIES:
// - Columns, the JSON lines and indexes
//
// IMPORTANT NOTES:
// - Read by warehouse while in transit
// =========================================================
internal sealed class OperationsSupplyTransferConfiguration : IEntityTypeConfiguration<OperationsSupplyTransfer>
{
    public void Configure(EntityTypeBuilder<OperationsSupplyTransfer> builder)
    {
        builder.ToTable("SupplyTransfers");
        builder.HasKey(transfer => transfer.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(transfer => transfer.InTransit);
        builder.Property(transfer => transfer.Note).HasMaxLength(OperationsSupplyStockLimits.NoteMaxLength);
        builder.OwnsMany(transfer => transfer.Lines, line => line.ToJson("Lines"));
        builder.Navigation(transfer => transfer.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(transfer => transfer.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(transfer => transfer.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(transfer => new { transfer.TenantId, transfer.ReceivedUtc });
    }
}
