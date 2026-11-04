using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplyConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupply to the database (Slice 5E).
//
// RESPONSIBILITIES:
// - Columns, the JSON prices and indexes
// - Its barcodes as their own rows
//
// IMPORTANT NOTES:
// - One item code per company
// =========================================================
internal sealed class OperationsSupplyConfiguration : IEntityTypeConfiguration<OperationsSupply>
{
    public void Configure(EntityTypeBuilder<OperationsSupply> builder)
    {
        builder.ToTable("Supplies");
        builder.HasKey(supply => supply.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(supply => supply.Code).HasMaxLength(OperationsSupplyLimits.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(supply => supply.NameEn).HasMaxLength(OperationsSupplyLimits.NameMaxLength).IsRequired();
        builder.Property(supply => supply.NameFr).HasMaxLength(OperationsSupplyLimits.NameMaxLength);
        builder.Property(supply => supply.NameEs).HasMaxLength(OperationsSupplyLimits.NameMaxLength);
        builder.Property(supply => supply.Unit).HasMaxLength(OperationsSupplyLimits.UnitMaxLength);
        builder.OwnsMany(supply => supply.Prices, price =>
        {
            price.ToJson("Prices");
            price.Property(item => item.Price).HasPrecision(12, 2);
            price.OwnsMany(item => item.Breaks, step => step.Property(value => value.Price).HasPrecision(12, 2));
            price.Navigation(item => item.Breaks).UsePropertyAccessMode(PropertyAccessMode.Field);
            price.OwnsMany(item => item.Warehouses, place => place.Property(value => value.Price).HasPrecision(12, 2));
            price.Navigation(item => item.Warehouses).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Navigation(supply => supply.Prices).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(supply => supply.Barcodes).WithOne().HasForeignKey(barcode => barcode.SupplyId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(supply => supply.Barcodes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(supply => new { supply.TenantId, supply.Code }).IsUnique();
    }
}
