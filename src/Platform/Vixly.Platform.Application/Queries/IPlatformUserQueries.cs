using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformUserQueries.cs
//
// PURPOSE:
// Read-only projections of staff users for screens.
//
// RESPONSIBILITIES:
// - Page the user list with search and status filter
// - Read one user's detail with roles in force, departments
//   and open sessions
// - Read the facts that decide which admin actions apply
//
// IMPORTANT NOTES:
// - No-tracking projections (P-1); commands use
//   IPlatformUserRepository instead
// - System roles carry their key; the presenter turns it into
//   text (L-1)
// =========================================================
public interface IPlatformUserQueries
{
    Task<PagedResult<PlatformUserListItem>> ListAsync(
        PlatformUserListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PlatformUserDetail?> GetAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<PlatformUserActionFacts?> GetActionFactsAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    // Member activity (owner, 2026-10-21): sign-ins to Control, newest first, of one user or all.
    Task<PagedResult<PlatformSignInItem>> ListSignInsAsync(
        Guid? platformUserId,
        DateTimeOffset utcNow,
        PagedRequest page,
        CancellationToken cancellationToken);
}
