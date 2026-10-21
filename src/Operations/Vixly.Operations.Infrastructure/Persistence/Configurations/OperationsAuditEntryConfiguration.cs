using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsAuditEntryConfiguration.cs
//
// PURPOSE:
// Maps the audit log to ops.AuditEntries.
//
// RESPONSIBILITIES:
// - Index by time, actor, target and action within the tenant
//
// IMPORTANT NOTES:
// - Append-only: the app login may only insert and read (global SQL script)
// =========================================================
internal sealed class OperationsAuditEntryConfiguration : IEntityTypeConfiguration<OperationsAuditEntry>
{
    public void Configure(EntityTypeBuilder<OperationsAuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.ActionCode).HasMaxLength(OperationsAuditEntry.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.PermissionCode).HasMaxLength(OperationsAuditEntry.CodeMaxLength).IsUnicode(false);
        builder.Property(entry => entry.TargetType).HasMaxLength(OperationsAuditEntry.TargetTypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.Reason).HasMaxLength(OperationsAuditEntry.ReasonMaxLength);
        builder.Property(entry => entry.IpAddress).HasMaxLength(OperationsIdentityLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(entry => entry.UserAgent).HasMaxLength(OperationsIdentityLimits.UserAgentMaxLength);
        builder.Property(entry => entry.CorrelationId).HasMaxLength(OperationsAuditEntry.CorrelationIdMaxLength).IsUnicode(false);
        builder.HasIndex(entry => new { entry.TenantId, entry.CreatedUtc });
        builder.HasIndex(entry => new { entry.TenantId, entry.ActorUserId, entry.CreatedUtc });
        builder.HasIndex(entry => new { entry.TenantId, entry.TargetId, entry.CreatedUtc });
    }
}
