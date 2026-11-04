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
// - Harmonization H-9 (owner, 2026-11-04): one read per action. The
//   answer is reused only by the same action of the same person (the
//   context's action stamp moves at each new action and each save),
//   and never after 5 seconds; every new action reads afresh, so a
//   removed right applies from the next action, as before
// =========================================================
internal sealed class PlatformPermissionReader : IPlatformPermissionReader
{
    private static readonly TimeSpan KeptAtMost = TimeSpan.FromSeconds(5);

    private readonly PlatformDbContext _context;

    private (Guid UserId, long Action, DateTimeOffset ReadAt, PlatformPermissionSnapshot? Snapshot)? _kept;

    public PlatformPermissionReader(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformPermissionSnapshot?> ReadAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_kept is { } kept && kept.UserId == platformUserId && kept.Action == _context.ActionStamp
            && utcNow >= kept.ReadAt && utcNow - kept.ReadAt < KeptAtMost)
        {
            return kept.Snapshot;
        }

        // The action is the one the read started in (review, 2026-11-04): a new action begun meanwhile reads again.
        long action = _context.ActionStamp;
        PlatformPermissionSnapshot? snapshot = await ReadNowAsync(platformUserId, utcNow, cancellationToken);
        _kept = (platformUserId, action, utcNow, snapshot);
        return snapshot;
    }

    private async Task<PlatformPermissionSnapshot?> ReadNowAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
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
