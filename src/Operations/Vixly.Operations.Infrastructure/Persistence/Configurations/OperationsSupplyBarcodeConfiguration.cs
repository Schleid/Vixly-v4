using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplyBarcodeConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupplyBarcode to the database (Slice 5E).
//
// RESPONSIBILITIES:
// - Columns and the unique index
//
// IMPORTANT NOTES:
// - A barcode belongs to one supply of the company
// =========================================================
internal sealed class OperationsSupplyBarcodeConfiguration : IEntityTypeConfiguration<OperationsSupplyBarcode>
{
    public void Configure(EntityTypeBuilder<OperationsSupplyBarcode> builder)
    {
        builder.ToTable("SupplyBarcodes");
        builder.HasKey(barcode => barcode.Id);
        builder.Property(barcode => barcode.Code).HasMaxLength(OperationsSupplyLimits.BarcodeMaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(barcode => new { barcode.TenantId, barcode.Code }).IsUnique();
    }
}
