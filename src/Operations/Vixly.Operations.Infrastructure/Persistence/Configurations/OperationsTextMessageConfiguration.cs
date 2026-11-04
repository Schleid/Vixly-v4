using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsTextMessageConfiguration.cs
//
// PURPOSE:
// Maps the log of text messages sent to customers (Slice 9).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - Never the text or the whole number
// =========================================================
internal sealed class OperationsTextMessageConfiguration : IEntityTypeConfiguration<OperationsTextMessage>
{
    public void Configure(EntityTypeBuilder<OperationsTextMessage> builder)
    {
        builder.ToTable("TextMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.ToHint).HasMaxLength(8).IsRequired();
        builder.Property(message => message.ProviderId).HasMaxLength(64).IsUnicode(false);
        builder.Property(message => message.ErrorCode).HasMaxLength(32).IsUnicode(false);
        builder.HasIndex(message => new { message.TenantId, message.CreatedUtc });
        builder.HasIndex(message => new { message.TenantId, message.ProviderId });
    }
}
