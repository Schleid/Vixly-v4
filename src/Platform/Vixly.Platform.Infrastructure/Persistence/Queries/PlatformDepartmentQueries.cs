using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformDepartmentQueries.cs
//
// PURPOSE:
// EF implementation of IPlatformDepartmentQueries.
//
// RESPONSIBILITIES:
// - Project departments, members and memberships for the
//   Departments screens and the departments section of a user
//
// IMPORTANT NOTES:
// - No tracking (P-1)
// - Member counts include every account state
// =========================================================
internal sealed class PlatformDepartmentQueries : IPlatformDepartmentQueries
{
    private readonly PlatformDbContext _context;

    public PlatformDepartmentQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PlatformDepartmentListItem>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        IQueryable<PlatformDepartment> departments = _context.Departments.AsNoTracking();

        if (!includeInactive)
        {
            departments = departments.Where(department => department.IsActive);
        }

        return await departments
            .OrderBy(department => department.Name)
            .Select(department => new PlatformDepartmentListItem(
                department.Id,
                department.Name,
                department.Description,
                department.IsActive,
                _context.Set<PlatformUserDepartment>().Count(membership => membership.PlatformDepartmentId == department.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<PlatformDepartmentDetail?> GetAsync(Guid platformDepartmentId, CancellationToken cancellationToken)
    {
        PlatformDepartment? department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == platformDepartmentId, cancellationToken);

        if (department is null)
        {
            return null;
        }

        List<PlatformDepartmentMemberItem> members = await (
                from membership in _context.Set<PlatformUserDepartment>().AsNoTracking()
                join user in _context.Users.AsNoTracking() on membership.PlatformUserId equals user.Id
                where membership.PlatformDepartmentId == platformDepartmentId
                orderby user.LastName, user.FirstName, user.Id
                select new PlatformDepartmentMemberItem(user.Id, user.FirstName, user.LastName, (string)(object)user.Email, membership.IsPrimary))
            .ToListAsync(cancellationToken);

        return new PlatformDepartmentDetail
        {
            PlatformDepartmentId = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedUtc = department.CreatedUtc,
            Members = members
        };
    }

    public async Task<IReadOnlyList<PlatformUserDepartmentItem>> ListUserMembershipsAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return await (
                from membership in _context.Set<PlatformUserDepartment>().AsNoTracking()
                join department in _context.Departments.AsNoTracking() on membership.PlatformDepartmentId equals department.Id
                where membership.PlatformUserId == platformUserId
                orderby membership.IsPrimary descending, department.Name
                select new PlatformUserDepartmentItem(department.Id, department.Name, department.IsActive, membership.IsPrimary))
            .ToListAsync(cancellationToken);
    }
}
