using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.SalesFeed;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedStateConfiguration.cs
//
// PURPOSE:
// Maps the sales feed's one state row.
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - One row, found by its key
// =========================================================
internal sealed class PlatformSalesFeedStateConfiguration : IEntityTypeConfiguration<PlatformSalesFeedState>
{
    public void Configure(EntityTypeBuilder<PlatformSalesFeedState> builder)
    {
        builder.ToTable("SalesFeedState");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Key).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.LastFailure).HasMaxLength(PlatformSalesFeedLimits.ProblemMaxLength).IsUnicode(false);
        builder.Property(item => item.Problem).HasMaxLength(PlatformSalesFeedLimits.ProblemMaxLength).IsUnicode(false);
        builder.Ignore(item => item.Stopped);
    }
}
