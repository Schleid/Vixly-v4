using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSessionRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformSessionRepository.
//
// RESPONSIBILITIES:
// - Find sessions by id and by token hash
// - List a user's open sessions
//
// IMPORTANT NOTES:
// - The token-hash lookup runs on every request and uses the
//   unique index on TokenHash
// =========================================================
internal sealed class PlatformSessionRepository : IPlatformSessionRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSessionRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformUserSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return _context.UserSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public Task<PlatformUserSession?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        return _context.UserSessions.FirstOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformUserSession>> ListOpenAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        return await _context.UserSessions
            .Where(session => session.PlatformUserId == platformUserId && session.EndedUtc == null)
            .ToListAsync(cancellationToken);
    }

    public void Add(PlatformUserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _context.UserSessions.Add(session);
    }
}
