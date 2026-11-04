using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.SalesFeed;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedEventConfiguration.cs
//
// PURPOSE:
// Maps the sale events for TechOS.
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - One row per source history row and one per totalled day (unique), so nothing is written twice
// - Sequence is unique: two runs at once cannot give the same number
// =========================================================
internal sealed class PlatformSalesFeedEventConfiguration : IEntityTypeConfiguration<PlatformSalesFeedEvent>
{
    public void Configure(EntityTypeBuilder<PlatformSalesFeedEvent> builder)
    {
        builder.ToTable("SalesFeedEvents");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.EventType).HasMaxLength(PlatformSalesFeedLimits.TypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false);
        builder.Property(item => item.Payload).IsRequired();
        builder.Property(item => item.LastError).HasMaxLength(PlatformSalesFeedLimits.ErrorMaxLength);
        builder.HasIndex(item => item.Sequence).IsUnique();
        builder.HasIndex(item => new { item.Source, item.SourceId }).IsUnique().HasFilter("[SourceId] IS NOT NULL");
        builder.HasIndex(item => item.TotalsDate).IsUnique().HasFilter("[TotalsDate] IS NOT NULL");
        builder.HasIndex(item => new { item.Status, item.Sequence });
        builder.HasIndex(item => item.OccurredUtc);
    }
}
