using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsBundleConfiguration.cs
//
// PURPOSE:
// Maps OperationsBundle to the database (Slice 6A).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - Its number is unique in the tenant; one form key makes one bundle (ADR-0019)
// =========================================================
internal sealed class OperationsBundleConfiguration : IEntityTypeConfiguration<OperationsBundle>
{
    public void Configure(EntityTypeBuilder<OperationsBundle> builder)
    {
        builder.ToTable("Bundles");
        builder.HasKey(bundle => bundle.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(bundle => bundle.Number).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(bundle => bundle.DestinationCountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(bundle => bundle.ServiceCode).HasMaxLength(Vixly.Operations.Domain.Charges.OperationsChargeLimits.ServiceCodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(bundle => bundle.TypeName).HasMaxLength(OperationsBundleLimits.TypeMaxLength);
        builder.Property(bundle => bundle.VoidReason).HasMaxLength(OperationsBundleLimits.ReasonMaxLength);
        builder.Property(bundle => bundle.Divisor).HasPrecision(12, 3);

        foreach (string weight in new[] { nameof(OperationsBundle.TargetKg), nameof(OperationsBundle.WeightKg), nameof(OperationsBundle.VolumetricKg) })
        {
            builder.Property<decimal?>(weight).HasPrecision(12, 3);
        }

        builder.Property(bundle => bundle.ParcelsWeightKg).HasPrecision(12, 3);
        builder.Property(bundle => bundle.ParcelsVolumetricKg).HasPrecision(12, 3);

        foreach (string side in new[] { nameof(OperationsBundle.LengthCm), nameof(OperationsBundle.WidthCm), nameof(OperationsBundle.HeightCm) })
        {
            builder.Property<decimal?>(side).HasPrecision(8, 1);
        }

        builder.Ignore(bundle => bundle.IsOpen);
        builder.Ignore(bundle => bundle.Filled);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(bundle => bundle.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(bundle => bundle.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(bundle => new { bundle.TenantId, bundle.Number }).IsUnique();
        builder.HasIndex(bundle => new { bundle.TenantId, bundle.RequestKey }).IsUnique().HasFilter("[RequestKey] IS NOT NULL");
        builder.HasIndex(bundle => new { bundle.TenantId, bundle.WarehouseId, bundle.Status });

        // Slice 6B: the dispatch it is manifested on.
        builder.HasOne<Vixly.Operations.Domain.Dispatch.OperationsDispatch>().WithMany().HasForeignKey(bundle => bundle.DispatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(bundle => new { bundle.TenantId, bundle.DispatchId }).HasFilter("[DispatchId] IS NOT NULL");

        // Slice 7: where it is now, once it arrived somewhere.
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(bundle => bundle.CurrentWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(bundle => new { bundle.TenantId, bundle.CurrentWarehouseId }).HasFilter("[CurrentWarehouseId] IS NOT NULL");
    }
}
