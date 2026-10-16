using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformNumberSequenceConfiguration.cs
//
// PURPOSE:
// Maps number sequences (D-B2).
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - One row per kind and year; the row version makes a race fail instead of repeating a number
// =========================================================
internal sealed class PlatformNumberSequenceConfiguration : IEntityTypeConfiguration<PlatformNumberSequence>
{
    public void Configure(EntityTypeBuilder<PlatformNumberSequence> builder)
    {
        builder.ToTable("NumberSequences");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => new { item.Kind, item.Year }).IsUnique();
    }
}
