using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsParcelPhotoConfiguration.cs
//
// PURPOSE:
// Maps parcel photos to ops.ParcelPhotos (Slice 3B).
//
// RESPONSIBILITIES:
// - Columns and the parcel link
//
// IMPORTANT NOTES:
// - The file lives in the file store; the row keeps its key, type, size and hash (D-R7)
// =========================================================
internal sealed class OperationsParcelPhotoConfiguration : IEntityTypeConfiguration<OperationsParcelPhoto>
{
    public void Configure(EntityTypeBuilder<OperationsParcelPhoto> builder)
    {
        builder.ToTable("ParcelPhotos");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Kind).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(item => item.StorageKey).HasMaxLength(OperationsReceivingLimits.StorageKeyMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(item => item.Sha256).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasIndex(item => new { item.TenantId, item.ParcelId });
        builder.HasOne<OperationsParcel>().WithMany().HasForeignKey(item => item.ParcelId).OnDelete(DeleteBehavior.Restrict);
    }
}
