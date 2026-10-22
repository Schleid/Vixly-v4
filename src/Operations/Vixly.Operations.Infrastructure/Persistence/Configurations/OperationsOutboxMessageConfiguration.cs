using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsOutboxMessageConfiguration.cs
//
// PURPOSE:
// Table ops.OutboxMessages.
//
// RESPONSIBILITIES:
// - Map the outbox
//
// IMPORTANT NOTES:
// - Indexes serve the due-message lease and stream order
// =========================================================
internal sealed class OperationsOutboxMessageConfiguration : IEntityTypeConfiguration<OperationsOutboxMessage>
{
    public void Configure(EntityTypeBuilder<OperationsOutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasMaxLength(OperationsOutboxMessage.TypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Payload).IsRequired();
        builder.Property(item => item.LastErrorCode).HasMaxLength(OperationsOutboxMessage.ErrorCodeMaxLength).IsUnicode(false);
        builder.Ignore(item => item.IsSetAside);
        builder.HasIndex(item => new { item.NextAttemptUtc, item.CreatedUtc }).HasFilter("[ProcessedUtc] IS NULL");
        builder.HasIndex(item => new { item.StreamId, item.CreatedUtc }).HasFilter("[ProcessedUtc] IS NULL");
    }
}
