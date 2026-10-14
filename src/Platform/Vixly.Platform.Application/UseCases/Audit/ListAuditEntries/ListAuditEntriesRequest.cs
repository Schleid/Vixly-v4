using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Audit.ListAuditEntries;

// =========================================================
// FILE: ListAuditEntriesRequest.cs
//
// PURPOSE:
// Input for ListAuditEntries.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ListAuditEntriesRequest
{
    public Guid? ActorUserId { get; init; }

    public Guid? TargetId { get; init; }

    public string? ActionCode { get; init; }

    public DateTimeOffset? FromUtc { get; init; }

    public DateTimeOffset? ToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
}
