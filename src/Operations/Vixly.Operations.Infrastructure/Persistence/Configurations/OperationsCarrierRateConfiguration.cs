using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Bundles;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCarrierRateConfiguration.cs
//
// PURPOSE:
// Maps carriers' rates to ops.CarrierRates (Slice 6B).
//
// RESPONSIBILITIES:
// - Amounts with four decimals
//
// IMPORTANT NOTES:
// - Removed only before it starts
// =========================================================
internal sealed class OperationsCarrierRateConfiguration : IEntityTypeConfiguration<OperationsCarrierRate>
{
    public void Configure(EntityTypeBuilder<OperationsCarrierRate> builder)
    {
        builder.ToTable("CarrierRates");
        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(rate => rate.BundleType).HasMaxLength(OperationsBundleLimits.TypeMaxLength);
        builder.Property(rate => rate.PerKg).HasPrecision(14, 4);
        builder.Property(rate => rate.Minimum).HasPrecision(14, 4);
        builder.Property(rate => rate.PerBundle).HasPrecision(14, 4);
        builder.HasOne<OperationsCarrier>().WithMany().HasForeignKey(rate => rate.CarrierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(rate => rate.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rate => new { rate.TenantId, rate.CarrierId, rate.CountryCode, rate.StartsOn });
    }
}
