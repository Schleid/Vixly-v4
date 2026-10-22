using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Audit;

// =========================================================
// FILE: OperationsAuditEntry.cs
//
// PURPOSE:
// One append-only audit entry of a tenant (Slice 1, as Platform D-2).
//
// RESPONSIBILITIES:
// - Check the action code and target
// - Clip free text to its column
//
// IMPORTANT NOTES:
// - Append-only: the app login has INSERT and SELECT only on the table
// - Written in the same commit as the change it records
// =========================================================
public sealed class OperationsAuditEntry : OperationsEntity
{
    public const int CodeMaxLength = 100;

    public const int TargetTypeMaxLength = 100;

    public const int CorrelationIdMaxLength = 64;

    public const int ReasonMaxLength = 500;

    private static readonly Regex CodePattern = new(@"^ops(\.[a-z][a-z0-9_]*)+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private OperationsAuditEntry()
    {
        ActionCode = string.Empty;
        TargetType = string.Empty;
    }

    private OperationsAuditEntry(Guid tenantId, DateTimeOffset occurredUtc)
        : base(tenantId, occurredUtc)
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

    public static OperationsAuditEntry Record(Guid tenantId, OperationsAuditRecord record, DateTimeOffset occurredUtc)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.ActionCode is null || record.ActionCode.Length > CodeMaxLength || !CodePattern.IsMatch(record.ActionCode)
            || (record.PermissionCode is not null && (record.PermissionCode.Length > CodeMaxLength || !CodePattern.IsMatch(record.PermissionCode))))
        {
            throw new DomainException(OperationsIdentityErrors.AuditActionInvalid);
        }

        if (string.IsNullOrWhiteSpace(record.TargetType) || record.TargetType.Length > TargetTypeMaxLength)
        {
            throw new DomainException(OperationsIdentityErrors.AuditTargetInvalid);
        }

        return new OperationsAuditEntry(tenantId, occurredUtc)
        {
            ActorUserId = record.ActorUserId,
            ActionCode = record.ActionCode,
            PermissionCode = record.PermissionCode,
            TargetType = record.TargetType,
            TargetId = record.TargetId,
            ArgumentsJson = record.ArgumentsJson,
            BeforeJson = record.BeforeJson,
            AfterJson = record.AfterJson,
            Reason = Clip(record.Reason, ReasonMaxLength),
            IpAddress = Clip(record.IpAddress, OperationsIdentityLimits.IpAddressMaxLength),
            UserAgent = Clip(record.UserAgent, OperationsIdentityLimits.UserAgentMaxLength),
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
