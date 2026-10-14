namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformAuditEntryDetail.cs
//
// PURPOSE:
// One audit entry with everything it recorded.
//
// RESPONSIBILITIES:
// - Carry the row plus permission, reason, values before and after,
//   arguments, browser and correlation id
//
// IMPORTANT NOTES:
// - JSON values are shown as recorded; they never hold secrets
// =========================================================
public sealed class PlatformAuditEntryDetail
{
    public PlatformAuditListItem Entry { get; init; } = null!;

    public string? PermissionCode { get; init; }

    public string? Reason { get; init; }

    public string? ArgumentsJson { get; init; }

    public string? BeforeJson { get; init; }

    public string? AfterJson { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }
}
