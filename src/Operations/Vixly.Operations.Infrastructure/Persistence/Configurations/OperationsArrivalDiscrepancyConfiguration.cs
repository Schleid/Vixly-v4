using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsArrivalDiscrepancyConfiguration.cs
//
// PURPOSE:
// Maps what did not match at a destination to ops.ArrivalDiscrepancies (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Kind, status, the code scanned or expected, the resolution
//
// IMPORTANT NOTES:
// - One open row per bundle or parcel and dispatch (filtered unique indexes)
// =========================================================
internal sealed class OperationsArrivalDiscrepancyConfiguration : IEntityTypeConfiguration<OperationsArrivalDiscrepancy>
{
    public void Configure(EntityTypeBuilder<OperationsArrivalDiscrepancy> builder)
    {
        builder.ToTable("ArrivalDiscrepancies");
        builder.HasKey(row => row.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(row => row.Code).HasMaxLength(OperationsArrivalDiscrepancy.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(row => row.Note).HasMaxLength(OperationsArrivalDiscrepancy.NoteMaxLength);
        builder.HasOne<OperationsDispatch>().WithMany().HasForeignKey(row => row.DispatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(row => row.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.Status, row.WarehouseId });
        builder.HasIndex(row => new { row.TenantId, row.DispatchId });
        builder.HasIndex(row => new { row.TenantId, row.DispatchId, row.BundleId }).IsUnique()
            .HasFilter("[Status] = 1 AND [BundleId] IS NOT NULL AND [ParcelId] IS NULL AND [DispatchId] IS NOT NULL");
        builder.HasIndex(row => new { row.TenantId, row.DispatchId, row.ParcelId }).IsUnique()
            .HasFilter("[Status] = 1 AND [ParcelId] IS NOT NULL AND [DispatchId] IS NOT NULL");
    }
}
