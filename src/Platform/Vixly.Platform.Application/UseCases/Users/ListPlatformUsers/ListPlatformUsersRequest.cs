using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Users.ListPlatformUsers;

// =========================================================
// FILE: ListPlatformUsersRequest.cs
//
// PURPOSE:
// Input for ListPlatformUsers.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Page size is capped at 100 (P-1)
// =========================================================
public sealed class ListPlatformUsersRequest
{
    public string? Search { get; init; }

    public PlatformUserAccountStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
}
