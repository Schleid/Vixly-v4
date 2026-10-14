using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Persistence;

namespace Vixly.Platform.Infrastructure.Security;

// =========================================================
// FILE: PlatformPermissionReader.cs
//
// PURPOSE:
// Reads a staff user's current access for the authorization
// service.
//
// RESPONSIBILITIES:
// - Read account state and authority level
// - Read the permission codes of active roles whose grants
//   are in force now
//
// IMPORTANT NOTES:
// - Two small no-tracking queries, both on indexed columns;
//   no entity is materialised
// =========================================================
internal sealed class PlatformPermissionReader : IPlatformPermissionReader
{
    private readonly PlatformDbContext _context;

    public PlatformPermissionReader(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformPermissionSnapshot?> ReadAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == platformUserId)
            .Select(candidate => new { candidate.AuthorityLevel, candidate.Account.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        List<string> codes = await PermissionCodesQuery(_context, platformUserId, utcNow)
            .ToListAsync(cancellationToken);

        return new PlatformPermissionSnapshot(
            user.Status == PlatformUserAccountStatus.Active,
            user.AuthorityLevel,
            codes.ToHashSet(StringComparer.Ordinal));
    }

    internal static IQueryable<string> PermissionCodesQuery(
        PlatformDbContext context,
        Guid platformUserId,
        DateTimeOffset utcNow)
    {
        return (
                from assignment in context.Set<PlatformRoleAssignment>().AsNoTracking()
                join role in context.Roles.AsNoTracking() on assignment.PlatformRoleId equals role.Id
                from permission in role.Permissions
                where assignment.PlatformUserId == platformUserId
                    && assignment.RevokedUtc == null
                    && (assignment.ExpiresUtc == null || assignment.ExpiresUtc > utcNow)
                    && role.IsActive
                select permission.PermissionCode)
            .Distinct();
    }
}
