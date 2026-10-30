using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsLabelPrinterConfiguration.cs
//
// PURPOSE:
// Maps label printers to ops.LabelPrinters (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns and the warehouse link
//
// IMPORTANT NOTES:
// - The address is a host and port, or a Windows printer name
// =========================================================
internal sealed class OperationsLabelPrinterConfiguration : IEntityTypeConfiguration<OperationsLabelPrinter>
{
    public void Configure(EntityTypeBuilder<OperationsLabelPrinter> builder)
    {
        builder.ToTable("LabelPrinters");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(OperationsLabelPrinter.NameMaxLength).IsRequired();
        builder.Property(item => item.Address).HasMaxLength(OperationsLabelPrinter.AddressMaxLength).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId });

        // Slice 4D step 2: printers before it hold ID labels.
        builder.Property(item => item.Media).HasDefaultValue(Vixly.Operations.Domain.Printing.OperationsLabelMedia.IdLabel);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
