using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsStorageLocationConfiguration.cs
//
// PURPOSE:
// Maps warehouse shelves to ops.StorageLocations (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns, the unique code per warehouse and the warehouse link
//
// IMPORTANT NOTES:
// - A code is unique within its warehouse
// =========================================================
internal sealed class OperationsStorageLocationConfiguration : IEntityTypeConfiguration<OperationsStorageLocation>
{
    public void Configure(EntityTypeBuilder<OperationsStorageLocation> builder)
    {
        builder.ToTable("StorageLocations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(OperationsReceivingLimits.LocationCodeMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.Code }).IsUnique();
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
