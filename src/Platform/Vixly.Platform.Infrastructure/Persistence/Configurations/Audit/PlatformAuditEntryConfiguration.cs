using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Audit;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Audit;

// =========================================================
// FILE: PlatformAuditEntryConfiguration.cs
//
// PURPOSE:
// Maps the append-only audit log to platform.AuditEntries.
//
// RESPONSIBILITIES:
// - Index for the audit screen filters: time, actor, target,
//   action
//
// IMPORTANT NOTES:
// - No foreign keys: the log must outlive anything it
//   mentions and must never block a change
// - The app login is granted INSERT and SELECT only (set in
//   the deployment SQL, not by EF)
// - CreatedUtc is the time the action happened
// =========================================================
internal sealed class PlatformAuditEntryConfiguration : IEntityTypeConfiguration<PlatformAuditEntry>
{
    public void Configure(EntityTypeBuilder<PlatformAuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.ActionCode).HasMaxLength(PlatformAuditEntry.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.PermissionCode).HasMaxLength(PlatformAuditEntry.CodeMaxLength).IsUnicode(false);
        builder.Property(entry => entry.TargetType).HasMaxLength(PlatformAuditEntry.TargetTypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.Reason).HasMaxLength(PlatformIdentityLimits.ReasonMaxLength);
        builder.Property(entry => entry.IpAddress).HasMaxLength(PlatformIdentityLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(entry => entry.UserAgent).HasMaxLength(PlatformIdentityLimits.UserAgentMaxLength);
        builder.Property(entry => entry.CorrelationId).HasMaxLength(PlatformAuditEntry.CorrelationIdMaxLength).IsUnicode(false);

        builder.HasIndex(entry => entry.CreatedUtc);
        builder.HasIndex(entry => new { entry.ActorUserId, entry.CreatedUtc });
        builder.HasIndex(entry => new { entry.TargetId, entry.CreatedUtc });
        builder.HasIndex(entry => new { entry.ActionCode, entry.CreatedUtc });
    }
}
