namespace Vixly.Operations.Application.UseCases.Audit.ListOperationsAuditEntries;

// =========================================================
// FILE: ListOperationsAuditEntriesRequest.cs
//
// PURPOSE:
// Lists the tenant's audit entries for the audit log screen (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsAuditEntriesRequest
{
    public Guid? ActorUserId { get; init; }

    public string? Area { get; init; }

    public DateTimeOffset? FromUtc { get; init; }

    public DateTimeOffset? ToUtc { get; init; }

    public int Page { get; init; } = 1;
}
