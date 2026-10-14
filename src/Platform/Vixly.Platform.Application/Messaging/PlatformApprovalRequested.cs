using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformApprovalRequested.cs
//
// PURPOSE:
// Payload of "approval requested" (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Say what waits, for which permission, and who asked
//
// IMPORTANT NOTES:
// - The handler emails every holder of the permission except the requester
// - Carries no reason; that stays in history and audit
// =========================================================
public sealed record PlatformApprovalRequested(
    PlatformApprovalKind Kind,
    Guid TargetId,
    string PermissionCode,
    string Title,
    Guid? RequestedByUserId,
    DateTimeOffset OccurredUtc);
