using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Messaging;

// =========================================================
// FILE: PlatformOutboxMessageConfiguration.cs
//
// PURPOSE:
// Maps outbox messages to platform.OutboxMessages (D-T17).
//
// RESPONSIBILITIES:
// - Type, stream, payload and delivery state
// - Indexes for leasing due messages and ordering per stream
//
// IMPORTANT NOTES:
// - The lease query lives in PlatformOutboxRepository
// =========================================================
internal sealed class PlatformOutboxMessageConfiguration : IEntityTypeConfiguration<PlatformOutboxMessage>
{
    public void Configure(EntityTypeBuilder<PlatformOutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasMaxLength(PlatformOutboxMessage.TypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Payload).IsRequired();
        builder.Property(item => item.LastErrorCode).HasMaxLength(PlatformOutboxMessage.ErrorCodeMaxLength).IsUnicode(false);
        builder.Ignore(item => item.IsSetAside);
        builder.HasIndex(item => new { item.NextAttemptUtc, item.CreatedUtc }).HasFilter("[ProcessedUtc] IS NULL");
        builder.HasIndex(item => new { item.StreamId, item.CreatedUtc }).HasFilter("[ProcessedUtc] IS NULL");
    }
}
