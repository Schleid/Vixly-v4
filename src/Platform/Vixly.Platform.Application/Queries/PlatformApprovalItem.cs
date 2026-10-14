namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformApprovalItem.cs
//
// PURPOSE:
// One request waiting for approval.
//
// RESPONSIBILITIES:
// - Carry one row for a console list
//
// IMPORTANT NOTES:
// - Read model; built by Infrastructure
// - TargetId is the plan, promotion or offer; for a discount it is the discount, and PlatformTenantId says whose
// =========================================================
public sealed record PlatformApprovalItem(
    PlatformApprovalKind Kind,
    Guid TargetId,
    Guid? PlatformTenantId,
    string Title,
    string? Detail,
    Guid? RequestedByUserId,
    string? RequestedByName,
    DateTimeOffset RequestedUtc,
    string? Reason);
