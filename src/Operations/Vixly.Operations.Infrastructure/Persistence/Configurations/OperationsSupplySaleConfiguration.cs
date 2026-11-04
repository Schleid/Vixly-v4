using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSupplySaleConfiguration.cs
//
// PURPOSE:
// Maps OperationsSupplySale to the database (Slice 5E step 3).
//
// RESPONSIBILITIES:
// - Columns, the JSON lines and indexes
//
// IMPORTANT NOTES:
// - An order's number is unique in the company; read by shipment and by customer
// =========================================================
internal sealed class OperationsSupplySaleConfiguration : IEntityTypeConfiguration<OperationsSupplySale>
{
    public void Configure(EntityTypeBuilder<OperationsSupplySale> builder)
    {
        builder.ToTable("SupplySales");
        builder.HasKey(sale => sale.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(sale => sale.Number).HasMaxLength(20).IsUnicode(false);
        builder.Property(sale => sale.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Ignore(sale => sale.Gross);
        builder.Ignore(sale => sale.DiscountTotal);
        builder.Ignore(sale => sale.Total);
        builder.OwnsMany(sale => sale.Lines, line =>
        {
            line.ToJson("Lines");
            line.Property(item => item.UnitPrice).HasPrecision(12, 2);
            line.Property(item => item.DiscountPercent).HasPrecision(5, 2);
            line.Ignore(item => item.Gross);
            line.Ignore(item => item.Discount);
            line.Ignore(item => item.Amount);
        });
        builder.Navigation(sale => sale.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(sale => sale.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(sale => sale.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsShipment>().WithMany().HasForeignKey(sale => sale.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(sale => new { sale.TenantId, sale.Number }).IsUnique().HasFilter("[Number] IS NOT NULL");
        // One live sale per shipment (review of 5E): two first scans at once cannot open two.
        builder.HasIndex(sale => new { sale.TenantId, sale.ShipmentId }).IsUnique().HasFilter("[ShipmentId] IS NOT NULL AND [Status] <> 3");
        builder.HasIndex(sale => new { sale.TenantId, sale.CustomerId, sale.Status });
    }
}
