using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Events;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsEventReasonConfiguration.cs
//
// PURPOSE:
// Maps the company's own causes to ops.EventReasons (Slice 6C).
//
// RESPONSIBILITIES:
// - Kind, code, words in three languages, active
//
// IMPORTANT NOTES:
// - The code is unique in the company
// =========================================================
internal sealed class OperationsEventReasonConfiguration : IEntityTypeConfiguration<OperationsEventReason>
{
    public void Configure(EntityTypeBuilder<OperationsEventReason> builder)
    {
        builder.ToTable("EventReasons");
        builder.HasKey(reason => reason.Id);
        builder.Property(reason => reason.Code).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.Property(reason => reason.TextEn).HasMaxLength(OperationsEventReason.TextMaxLength).IsRequired();
        builder.Property(reason => reason.TextFr).HasMaxLength(OperationsEventReason.TextMaxLength);
        builder.Property(reason => reason.TextEs).HasMaxLength(OperationsEventReason.TextMaxLength);
        builder.HasIndex(reason => new { reason.TenantId, reason.Code }).IsUnique();
    }
}
