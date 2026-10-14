namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformAuditFilter.cs
//
// PURPOSE:
// The filters of the audit log.
//
// RESPONSIBILITIES:
// - Carry who acted, on what, which action and the time window
//
// IMPORTANT NOTES:
// - All optional; ToUtc is exclusive
// =========================================================
public sealed record PlatformAuditFilter(
    Guid? ActorUserId,
    Guid? TargetId,
    string? ActionCode,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);
