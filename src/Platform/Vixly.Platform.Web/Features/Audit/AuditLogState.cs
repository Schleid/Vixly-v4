using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: AuditLogState.cs
//
// PURPOSE:
// What the audit log shows.
//
// RESPONSIBILITIES:
// - Hold one page of entries, the paging numbers and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record AuditLogState(
    bool Loaded,
    IReadOnlyList<PlatformAuditListItem> Entries,
    int Page,
    int TotalPages,
    int TotalCount,
    Error? Error)
{
    public static AuditLogState Initial { get; } = new(false, Array.Empty<PlatformAuditListItem>(), 1, 0, 0, null);
}
