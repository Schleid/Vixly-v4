using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsShelfCountConfiguration.cs
//
// PURPOSE:
// Maps shelf counts (Slice 12D) to ops.ShelfCounts.
//
// RESPONSIBILITIES:
// - One open count per shelf (unique filtered index); a row version for two scanners at once
//
// IMPORTANT NOTES:
// - The lists are text, one code per line (at most 500 x 40 characters)
// =========================================================
internal sealed class OperationsShelfCountConfiguration : IEntityTypeConfiguration<OperationsShelfCount>
{
    public void Configure(EntityTypeBuilder<OperationsShelfCount> builder)
    {
        builder.ToTable("ShelfCounts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.LocationCode).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Scanned).IsUnicode(false).IsRequired();
        builder.Property(item => item.Found).IsUnicode(false);
        builder.Property(item => item.Missing).IsUnicode(false);
        builder.Property(item => item.Extra).IsUnicode(false);
        builder.Property(item => item.Unknown).IsUnicode(false);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.IsOpen);
        builder.Ignore(item => item.ScannedCodes);
        builder.HasIndex(item => new { item.TenantId, item.LocationId }).IsUnique().HasFilter("[FinishedUtc] IS NULL");
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.CreatedUtc });
        builder.HasOne<OperationsStorageLocation>().WithMany().HasForeignKey(item => item.LocationId).OnDelete(DeleteBehavior.NoAction);
    }
}
