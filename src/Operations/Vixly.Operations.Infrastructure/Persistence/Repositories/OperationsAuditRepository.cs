using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Audit;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsAuditRepository.cs
//
// PURPOSE:
// The append-only audit log.
//
// RESPONSIBILITIES:
// - Implement IOperationsAuditRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered by the query filter (D-F1)
// =========================================================
internal sealed class OperationsAuditRepository : IOperationsAuditRepository, IOperationsAuditReader
{
    private readonly OperationsDbContext _context;

    public OperationsAuditRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _context.AuditEntries.Add(entry);
    }

    public async Task<(IReadOnlyList<OperationsAuditEntry> Items, int TotalCount)> ListAsync(OperationsAuditFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IQueryable<OperationsAuditEntry> query = _context.AuditEntries.AsNoTracking();

        if (filter.ActorUserId is { } actor)
        {
            query = query.Where(entry => entry.ActorUserId == actor);
        }

        if (!string.IsNullOrWhiteSpace(filter.Area))
        {
            string prefix = "ops." + filter.Area.Trim() + ".";
            query = query.Where(entry => entry.ActionCode.StartsWith(prefix));
        }

        if (filter.FromUtc is { } from)
        {
            query = query.Where(entry => entry.CreatedUtc >= from);
        }

        if (filter.ToUtc is { } to)
        {
            query = query.Where(entry => entry.CreatedUtc < to);
        }

        int total = await query.CountAsync(cancellationToken);
        List<OperationsAuditEntry> items = await query
            .OrderByDescending(entry => entry.CreatedUtc)
            .Skip((Math.Max(page, 1) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
