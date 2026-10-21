using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCashDrawerConfiguration.cs
//
// PURPOSE:
// Maps OperationsCashDrawer to the database (Slice 5F step 2).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One open drawer per person and warehouse
// =========================================================
internal sealed class OperationsCashDrawerConfiguration : IEntityTypeConfiguration<OperationsCashDrawer>
{
    public void Configure(EntityTypeBuilder<OperationsCashDrawer> builder)
    {
        builder.ToTable("CashDrawers");
        builder.HasKey(drawer => drawer.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        foreach (string money in new[] { nameof(OperationsCashDrawer.Float), nameof(OperationsCashDrawer.CashIn), nameof(OperationsCashDrawer.CashOut) })
        {
            builder.Property<decimal>(money).HasPrecision(14, 2);
        }

        builder.Property(drawer => drawer.Counted).HasPrecision(14, 2);
        builder.Property(drawer => drawer.Difference).HasPrecision(14, 2);
        builder.Property(drawer => drawer.CloseNote).HasMaxLength(OperationsBillingLimits.ReasonMaxLength);
        builder.Ignore(drawer => drawer.Expected);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(drawer => drawer.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(drawer => new { drawer.TenantId, drawer.UserId, drawer.WarehouseId }).IsUnique().HasFilter("[Status] = 1");
        builder.HasIndex(drawer => new { drawer.TenantId, drawer.WarehouseId, drawer.OpenedUtc });
    }
}
