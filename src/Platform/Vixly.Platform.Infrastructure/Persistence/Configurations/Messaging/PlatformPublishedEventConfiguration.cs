using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Messaging;

// =========================================================
// FILE: PlatformPublishedEventConfiguration.cs
//
// PURPOSE:
// Maps the public event feed (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Table PublishedEvents with a growing sequence
//
// IMPORTANT NOTES:
// - Sequence is an identity column with a unique index; the key stays the id
// - One row per outbox message and type
// =========================================================
internal sealed class PlatformPublishedEventConfiguration : IEntityTypeConfiguration<PlatformPublishedEvent>
{
    public void Configure(EntityTypeBuilder<PlatformPublishedEvent> builder)
    {
        builder.ToTable("PublishedEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Sequence).UseIdentityColumn().ValueGeneratedOnAdd();
        builder.Property(item => item.Sequence).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        builder.Property(item => item.Type).HasMaxLength(PlatformPublishedEvent.TypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Payload).IsRequired();
        builder.HasIndex(item => item.Sequence).IsUnique();
        builder.HasIndex(item => new { item.OutboxMessageId, item.Type }).IsUnique();
        builder.HasIndex(item => item.CreatedUtc);
    }
}
