using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsInspectionConfiguration.cs
//
// PURPOSE:
// Maps OperationsInspection to the database (Slice 3C).
//
// RESPONSIBILITIES:
// - Columns, indexes and links
//
// IMPORTANT NOTES:
// - A row version: two inspectors finishing at once clash, one is refused
// - Status 3 is Done (OperationsInspectionStatus)
// =========================================================
internal sealed class OperationsInspectionConfiguration : IEntityTypeConfiguration<OperationsInspection>
{
    public void Configure(EntityTypeBuilder<OperationsInspection> builder)
    {
        builder.ToTable("Inspections");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Why).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(OperationsInspectionLimits.ReasonMaxLength);
        builder.Property(item => item.Result).HasMaxLength(40).IsUnicode(false);
        builder.Property(item => item.ItemsFound).HasMaxLength(OperationsInspectionLimits.ItemsFoundMaxLength);
        builder.Property(item => item.Notes).HasMaxLength(OperationsInspectionLimits.NotesMaxLength);
        builder.Property(item => item.CustomerAction).HasMaxLength(OperationsInspectionLimits.CustomerActionMaxLength);
        builder.Property(item => item.Fee).HasPrecision(12, 2);
        builder.Property(item => item.FeeCurrency).HasMaxLength(3).IsUnicode(false);
        builder.Property(item => item.CallNotes).HasMaxLength(OperationsInspectionLimits.CallNotesMaxLength);
        builder.Ignore(item => item.IsOpen);

        // One open inspection per parcel; the queue reads by warehouse and status.
        builder.HasIndex(item => new { item.TenantId, item.ParcelId }).IsUnique().HasFilter("[Status] <> 3").HasDatabaseName("IX_Inspections_TenantId_ParcelId_Open");
        builder.HasIndex(item => new { item.TenantId, item.ParcelId, item.CreatedUtc });
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.Status, item.CreatedUtc });
        builder.HasOne<OperationsParcel>().WithMany().HasForeignKey(item => item.ParcelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
