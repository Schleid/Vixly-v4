using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Audit;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformAuditQueries.cs
//
// PURPOSE:
// EF implementation of IPlatformAuditQueries.
//
// RESPONSIBILITIES:
// - Filter and page audit entries, newest first
// - Look up the names of the actor and the target
//
// IMPORTANT NOTES:
// - No tracking (P-1)
// - Filters use the indexes on (ActorUserId, CreatedUtc),
//   (TargetId, CreatedUtc), (ActionCode, CreatedUtc) and CreatedUtc
// - Names are correlated lookups on the page of rows only
// =========================================================
internal sealed class PlatformAuditQueries : IPlatformAuditQueries
{
    private readonly PlatformDbContext _context;

    public PlatformAuditQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformAuditListItem>> ListAsync(
        PlatformAuditFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformAuditEntry> entries = _context.AuditEntries.AsNoTracking();

        if (filter.ActorUserId is Guid actor)
        {
            entries = entries.Where(entry => entry.ActorUserId == actor);
        }

        if (filter.TargetId is Guid target)
        {
            entries = entries.Where(entry => entry.TargetId == target);
        }

        if (filter.ActionCode is string actionCode)
        {
            entries = entries.Where(entry => entry.ActionCode == actionCode);
        }

        if (filter.FromUtc is DateTimeOffset from)
        {
            entries = entries.Where(entry => entry.CreatedUtc >= from);
        }

        if (filter.ToUtc is DateTimeOffset to)
        {
            entries = entries.Where(entry => entry.CreatedUtc < to);
        }

        int total = await entries.CountAsync(cancellationToken);

        List<PlatformAuditListItem> items = await Project(entries
                .OrderByDescending(entry => entry.CreatedUtc)
                .ThenByDescending(entry => entry.Id)
                .Skip(page.Skip)
                .Take(page.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformAuditListItem>(items, page, total);
    }

    public async Task<PlatformAuditEntryDetail?> GetAsync(Guid auditEntryId, CancellationToken cancellationToken)
    {
        IQueryable<PlatformAuditEntry> entry = _context.AuditEntries.AsNoTracking().Where(candidate => candidate.Id == auditEntryId);

        PlatformAuditListItem? item = await Project(entry).FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        PlatformAuditEntry row = await entry.FirstAsync(cancellationToken);

        return new PlatformAuditEntryDetail
        {
            Entry = item,
            PermissionCode = row.PermissionCode,
            Reason = row.Reason,
            ArgumentsJson = row.ArgumentsJson,
            BeforeJson = row.BeforeJson,
            AfterJson = row.AfterJson,
            UserAgent = row.UserAgent,
            CorrelationId = row.CorrelationId
        };
    }

    private IQueryable<PlatformAuditListItem> Project(IQueryable<PlatformAuditEntry> entries)
    {
        return entries.Select(entry => new PlatformAuditListItem(
            entry.Id,
            entry.CreatedUtc,
            entry.ActorUserId,
            _context.Users
                .Where(user => user.Id == entry.ActorUserId)
                .Select(user => user.FirstName + " " + user.LastName)
                .FirstOrDefault(),
            entry.ActionCode,
            entry.TargetType,
            entry.TargetId,
            entry.TargetType == nameof(PlatformUser)
                ? _context.Users.Where(user => user.Id == entry.TargetId).Select(user => user.FirstName + " " + user.LastName).FirstOrDefault()
                : entry.TargetType == nameof(PlatformRole)
                    ? _context.Roles.Where(role => role.Id == entry.TargetId).Select(role => role.Name).FirstOrDefault()
                    : entry.TargetType == nameof(PlatformDepartment)
                        ? _context.Departments.Where(department => department.Id == entry.TargetId).Select(department => department.Name).FirstOrDefault()
                        : null,
            entry.TargetType == nameof(PlatformRole)
                ? _context.Roles.Where(role => role.Id == entry.TargetId).Select(role => role.SystemKey).FirstOrDefault()
                : null,
            entry.IpAddress));
    }
}
