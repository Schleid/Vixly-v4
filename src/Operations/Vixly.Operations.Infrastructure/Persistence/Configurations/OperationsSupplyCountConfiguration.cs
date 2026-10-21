using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplyCountConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupplyCount to the database (Slice 5E step 2).
//
// RESPONSIBILITIES:
// - Columns, the JSON lines and indexes
//
// IMPORTANT NOTES:
// - One open count per warehouse is checked by the use case
// =========================================================
internal sealed class OperationsSupplyCountConfiguration : IEntityTypeConfiguration<OperationsSupplyCount>
{
    public void Configure(EntityTypeBuilder<OperationsSupplyCount> builder)
    {
        builder.ToTable("SupplyCounts");
        builder.HasKey(count => count.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(count => count.IsOpen);
        builder.OwnsMany(count => count.Lines, line => line.ToJson("Lines"));
        builder.Navigation(count => count.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(count => count.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(count => new { count.TenantId, count.WarehouseId, count.ClosedUtc });
    }
}
