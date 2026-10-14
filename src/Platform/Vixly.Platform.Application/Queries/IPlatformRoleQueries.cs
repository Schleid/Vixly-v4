namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformRoleQueries.cs
//
// PURPOSE:
// Read-only projections for the Roles screens and the roles
// section of a user.
//
// RESPONSIBILITIES:
// - List roles with their permissions and holder counts
// - Load one role with its holders
// - List who holds a permission, and the grants of one user
//
// IMPORTANT NOTES:
// - No tracking; never used to change data
// - A grant counts when it is not revoked and not expired at
//   utcNow
// =========================================================
public interface IPlatformRoleQueries
{
    Task<IReadOnlyList<PlatformRoleListItem>> ListAsync(
        bool includeInactive,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<PlatformRoleDetail?> GetAsync(
        Guid platformRoleId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformPermissionHolderItem>> ListPermissionHoldersAsync(
        string permissionCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformUserRoleItem>> ListUserGrantsAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
