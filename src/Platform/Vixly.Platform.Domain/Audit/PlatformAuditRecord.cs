namespace Vixly.Platform.Domain.Audit;

// =========================================================
// FILE: PlatformAuditRecord.cs
//
// PURPOSE:
// The values a use case supplies to write one audit entry.
//
// RESPONSIBILITIES:
// - Group the audit fields so PlatformAuditEntry.Record has
//   one argument instead of twelve
//
// IMPORTANT NOTES:
// - JSON fields are produced by the Application layer with
//   System.Text.Json; the domain stores them as given
// - ActorUserId is null only for anonymous events (sign-in
//   attempts, password-reset requests)
// =========================================================
public sealed record PlatformAuditRecord
{
    public Guid? ActorUserId { get; init; }

    public required string ActionCode { get; init; }

    public string? PermissionCode { get; init; }

    public required string TargetType { get; init; }

    public Guid? TargetId { get; init; }

    public string? ArgumentsJson { get; init; }

    public string? BeforeJson { get; init; }

    public string? AfterJson { get; init; }

    public string? Reason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }
}
