using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformAccountTokenRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformAccountTokenRepository.
//
// RESPONSIBILITIES:
// - Find by token hash; list unused links; add
//
// IMPORTANT NOTES:
// - Uses the unique index on TokenHash and the index on
//   (PlatformUserId, Purpose)
// =========================================================
internal sealed class PlatformAccountTokenRepository : IPlatformAccountTokenRepository
{
    private readonly PlatformDbContext _context;

    public PlatformAccountTokenRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformAccountToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        return _context.AccountTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformAccountToken>> ListUnusedAsync(
        Guid platformUserId,
        PlatformAccountTokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        return await _context.AccountTokens
            .Where(token => token.PlatformUserId == platformUserId
                && token.Purpose == purpose
                && token.UsedUtc == null
                && token.RevokedUtc == null)
            .ToListAsync(cancellationToken);
    }

    public void Add(PlatformAccountToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _context.AccountTokens.Add(token);
    }
}
