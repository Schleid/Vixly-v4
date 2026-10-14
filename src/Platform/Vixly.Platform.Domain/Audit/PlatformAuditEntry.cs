using System.Text.RegularExpressions;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Audit;

// =========================================================
// FILE: PlatformAuditEntry.cs
//
// PURPOSE:
// An append-only record of one command run in the Platform
// console: who did what, to what, why, and from where.
//
// RESPONSIBILITIES:
// - Hold the actor, the permission used, the action code,
//   the target, the arguments and before/after snapshots
// - Hold the client IP, user agent, reason and correlation id
//
// IMPORTANT NOTES:
// - Aggregate root with no mutating methods. The app's SQL
//   login has INSERT and SELECT only on this table
// - The action is a code plus JSON arguments, never a
//   sentence, so it reads correctly in every language (D-4).
//   The code is the event (platform.users.locked), not the
//   permission
// - Written in the same SaveChanges as the change it records
//   (D-2)
// - Snapshots never contain secrets: no password hashes,
//   token hashes, passkey keys or authenticator secrets
// =========================================================
public sealed class PlatformAuditEntry : AuditableEntity
{
    public const int CodeMaxLength = 100;

    public const int TargetTypeMaxLength = 100;

    public const int CorrelationIdMaxLength = 64;

    private static readonly Regex CodePattern = new(
        @"^platform(\.[a-z][a-z0-9_]*)+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private PlatformAuditEntry()
    {
        ActionCode = string.Empty;
        TargetType = string.Empty;
    }

    private PlatformAuditEntry(DateTimeOffset occurredUtc)
        : base(occurredUtc)
    {
        ActionCode = string.Empty;
        TargetType = string.Empty;
    }

    public Guid? ActorUserId { get; private set; }

    public string ActionCode { get; private set; }

    public string? PermissionCode { get; private set; }

    public string TargetType { get; private set; }

    public Guid? TargetId { get; private set; }

    public string? ArgumentsJson { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public string? Reason { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? CorrelationId { get; private set; }

    // =====================================================
    // METHOD: RECORD
    // =====================================================
    public static PlatformAuditEntry Record(PlatformAuditRecord record, DateTimeOffset occurredUtc)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.ActionCode is null
            || record.ActionCode.Length > CodeMaxLength
            || !CodePattern.IsMatch(record.ActionCode)
            || (record.PermissionCode is not null
                && (record.PermissionCode.Length > CodeMaxLength || !CodePattern.IsMatch(record.PermissionCode))))
        {
            throw new DomainException(PlatformIdentityErrors.AuditActionInvalid);
        }

        if (string.IsNullOrWhiteSpace(record.TargetType) || record.TargetType.Length > TargetTypeMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.AuditTargetInvalid);
        }

        return new PlatformAuditEntry(occurredUtc)
        {
            ActorUserId = record.ActorUserId,
            ActionCode = record.ActionCode,
            PermissionCode = record.PermissionCode,
            TargetType = record.TargetType,
            TargetId = record.TargetId,
            ArgumentsJson = record.ArgumentsJson,
            BeforeJson = record.BeforeJson,
            AfterJson = record.AfterJson,
            Reason = Clip(record.Reason, PlatformIdentityLimits.ReasonMaxLength),
            IpAddress = Clip(record.IpAddress, PlatformIdentityLimits.IpAddressMaxLength),
            UserAgent = Clip(record.UserAgent, PlatformIdentityLimits.UserAgentMaxLength),
            CorrelationId = Clip(record.CorrelationId, CorrelationIdMaxLength)
        };
    }

    private static string? Clip(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
