namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformDepartmentQueries.cs
//
// PURPOSE:
// Read-only projections for the Departments screens and the
// departments section of a user.
//
// RESPONSIBILITIES:
// - List departments with member counts
// - Load one department with its members
// - List the memberships of one user
//
// IMPORTANT NOTES:
// - No tracking; never used to change data
// =========================================================
public interface IPlatformDepartmentQueries
{
    Task<IReadOnlyList<PlatformDepartmentListItem>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<PlatformDepartmentDetail?> GetAsync(Guid platformDepartmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformUserDepartmentItem>> ListUserMembershipsAsync(Guid platformUserId, CancellationToken cancellationToken);
}
