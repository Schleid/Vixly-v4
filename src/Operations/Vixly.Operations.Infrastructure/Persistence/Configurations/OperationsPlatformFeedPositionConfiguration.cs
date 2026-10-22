using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Platform;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsPlatformFeedPositionConfiguration.cs
//
// PURPOSE:
// Table ops.PlatformFeedPositions.
//
// RESPONSIBILITIES:
// - Map feed positions
//
// IMPORTANT NOTES:
// - One row per feed (unique); row version stops two hosts moving it at once
// =========================================================
internal sealed class OperationsPlatformFeedPositionConfiguration : IEntityTypeConfiguration<OperationsPlatformFeedPosition>
{
    public void Configure(EntityTypeBuilder<OperationsPlatformFeedPosition> builder)
    {
        builder.ToTable("PlatformFeedPositions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Feed).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Feed).IsUnique();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
