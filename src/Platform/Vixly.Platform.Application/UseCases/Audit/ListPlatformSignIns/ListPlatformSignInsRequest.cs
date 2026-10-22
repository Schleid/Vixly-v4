using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Audit.ListPlatformSignIns;

// =========================================================
// FILE: ListPlatformSignInsRequest.cs
//
// PURPOSE:
// Input of ListPlatformSignIns: member activity in Control (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case
// =========================================================
public sealed class ListPlatformSignInsRequest
{
    public Guid? PlatformUserId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
}
