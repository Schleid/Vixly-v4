namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformAuditListItem.cs
//
// PURPOSE:
// One row of the audit log.
//
// RESPONSIBILITIES:
// - Carry when, who, what, on which target and from where
//
// IMPORTANT NOTES:
// - Names are looked up now: a renamed role shows its current name
// - ActorName is null for actions without a signed-in actor
//   (sign-in attempts, the deploy)
// =========================================================
public sealed record PlatformAuditListItem(
    Guid AuditEntryId,
    DateTimeOffset OccurredUtc,
    Guid? ActorUserId,
    string? ActorName,
    string ActionCode,
    string TargetType,
    Guid? TargetId,
    string? TargetName,
    string? TargetSystemKey,
    string? IpAddress);
