using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformRoleQueries.cs
//
// PURPOSE:
// EF implementation of IPlatformRoleQueries.
//
// RESPONSIBILITIES:
// - Project roles, holders and grants for the Roles screens and
//   the roles section of a user
//
// IMPORTANT NOTES:
// - No tracking (P-1)
// - A grant is in force when it is not revoked and not expired;
//   holder counts include every account state
// - Permission holders are active accounts only: other accounts'
//   access is refused at the gate anyway
// - System roles sort first, then by name
// =========================================================
internal sealed class PlatformRoleQueries : IPlatformRoleQueries
{
    private readonly PlatformDbContext _context;

    public PlatformRoleQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PlatformRoleListItem>> ListAsync(
        bool includeInactive,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        IQueryable<PlatformRole> roles = _context.Roles.AsNoTracking();

        if (!includeInactive)
        {
            roles = roles.Where(role => role.IsActive);
        }

        var rows = await roles
            .OrderBy(role => role.SystemKey == null)
            .ThenBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                role.Name,
                role.SystemKey,
                role.IsActive,
                Codes = role.Permissions.Select(permission => permission.PermissionCode).ToList()
            })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, int> holders = await GrantsInForce(utcNow)
            .GroupBy(assignment => assignment.PlatformRoleId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

        return rows
            .Select(row => new PlatformRoleListItem(
                row.Id,
                row.Name,
                row.SystemKey,
                row.IsActive,
                row.Codes.OrderBy(code => code, StringComparer.Ordinal).ToList(),
                holders.GetValueOrDefault(row.Id)))
            .ToList();
    }

    public async Task<PlatformRoleDetail?> GetAsync(
        Guid platformRoleId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(candidate => candidate.Id == platformRoleId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
                candidate.SystemKey,
                candidate.IsActive,
                candidate.CreatedUtc,
                Codes = candidate.Permissions.Select(permission => permission.PermissionCode).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        List<PlatformRoleHolderItem> holders = await (
                from assignment in GrantsInForce(utcNow)
                join user in _context.Users.AsNoTracking() on assignment.PlatformUserId equals user.Id
                where assignment.PlatformRoleId == platformRoleId
                orderby user.LastName, user.FirstName, user.Id
                select new PlatformRoleHolderItem(
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    (string)(object)user.Email,
                    assignment.CreatedUtc,
                    assignment.ExpiresUtc))
            .ToListAsync(cancellationToken);

        return new PlatformRoleDetail
        {
            PlatformRoleId = role.Id,
            Name = role.Name,
            SystemKey = role.SystemKey,
            IsActive = role.IsActive,
            CreatedUtc = role.CreatedUtc,
            PermissionCodes = role.Codes.OrderBy(code => code, StringComparer.Ordinal).ToList(),
            Holders = holders
        };
    }

    public async Task<IReadOnlyList<PlatformPermissionHolderItem>> ListPermissionHoldersAsync(
        string permissionCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        List<PlatformPermissionHolderItem> throughRoles = await (
                from assignment in GrantsInForce(utcNow)
                join role in _context.Roles.AsNoTracking() on assignment.PlatformRoleId equals role.Id
                join user in _context.Users.AsNoTracking() on assignment.PlatformUserId equals user.Id
                where role.IsActive
                    && role.Permissions.Any(permission => permission.PermissionCode == permissionCode)
                    && user.Account.Status == PlatformUserAccountStatus.Active
                select new PlatformPermissionHolderItem(
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    (string)(object)user.Email,
                    user.AuthorityLevel,
                    role.Name,
                    role.SystemKey,
                    assignment.ExpiresUtc,
                    (string)(object)user.PreferredLanguage))
            .ToListAsync(cancellationToken);

        List<PlatformPermissionHolderItem> superUsers = await _context.Users
            .AsNoTracking()
            .Where(user => user.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser
                && user.Account.Status == PlatformUserAccountStatus.Active)
            .Select(user => new PlatformPermissionHolderItem(
                user.Id,
                user.FirstName,
                user.LastName,
                (string)(object)user.Email,
                user.AuthorityLevel,
                null,
                null,
                null,
                (string)(object)user.PreferredLanguage))
            .ToListAsync(cancellationToken);

        return superUsers
            .Concat(throughRoles)
            .OrderBy(holder => holder.LastName, StringComparer.CurrentCulture)
            .ThenBy(holder => holder.FirstName, StringComparer.CurrentCulture)
            .ThenBy(holder => holder.RoleName is not null)
            .ToList();
    }

    public async Task<IReadOnlyList<PlatformUserRoleItem>> ListUserGrantsAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        return await (
                from assignment in GrantsInForce(utcNow)
                join role in _context.Roles.AsNoTracking() on assignment.PlatformRoleId equals role.Id
                where assignment.PlatformUserId == platformUserId
                orderby role.SystemKey == null, role.Name
                select new PlatformUserRoleItem(
                    role.Id,
                    role.Name,
                    role.SystemKey,
                    role.IsActive,
                    assignment.CreatedUtc,
                    assignment.ExpiresUtc))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<PlatformRoleAssignment> GrantsInForce(DateTimeOffset utcNow)
    {
        return _context.Set<PlatformRoleAssignment>()
            .AsNoTracking()
            .Where(assignment => assignment.RevokedUtc == null
                && (assignment.ExpiresUtc == null || assignment.ExpiresUtc > utcNow));
    }
}
