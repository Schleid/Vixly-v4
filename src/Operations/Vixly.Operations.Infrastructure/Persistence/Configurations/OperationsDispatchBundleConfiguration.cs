using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDispatchBundleConfiguration.cs
//
// PURPOSE:
// Maps the bundles a dispatch carried to ops.DispatchBundles (Slice 7, R-11).
//
// RESPONSIBILITIES:
// - One row per bundle per dispatch
//
// IMPORTANT NOTES:
// - A bundle may be on two dispatches: to a hub, then onward
// =========================================================
internal sealed class OperationsDispatchBundleConfiguration : IEntityTypeConfiguration<OperationsDispatchBundle>
{
    public void Configure(EntityTypeBuilder<OperationsDispatchBundle> builder)
    {
        builder.ToTable("DispatchBundles");
        builder.HasKey(row => row.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne<OperationsDispatch>().WithMany().HasForeignKey(row => row.DispatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsBundle>().WithMany().HasForeignKey(row => row.BundleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.TenantId, row.DispatchId, row.BundleId }).IsUnique();
        builder.HasIndex(row => new { row.TenantId, row.BundleId });
    }
}
