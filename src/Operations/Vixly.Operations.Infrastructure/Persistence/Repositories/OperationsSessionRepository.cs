using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsSessionRepository.cs
//
// PURPOSE:
// Staff sessions of the current tenant.
//
// RESPONSIBILITIES:
// - Implement IOperationsSessionRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant-filtered by the query filter (D-F1)
// =========================================================
internal sealed class OperationsSessionRepository : IOperationsSessionRepository
{
    private readonly OperationsDbContext _context;

    public OperationsSessionRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsUserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _context.UserSessions.Add(session);
    }

    public Task<OperationsUserSession?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _context.UserSessions.FirstOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);
    }

    public Task<OperationsUserSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return _context.UserSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsUserSession>> ListOpenAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.UserSessions.Where(session => session.UserId == userId && session.EndedUtc == null).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OperationsUserSession> Items, int TotalCount)> ListActivityAsync(Guid? userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<OperationsUserSession> query = _context.UserSessions.AsNoTracking();

        if (userId is { } id)
        {
            query = query.Where(session => session.UserId == id);
        }

        int total = await query.CountAsync(cancellationToken);
        List<OperationsUserSession> items = await query.OrderByDescending(session => session.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
