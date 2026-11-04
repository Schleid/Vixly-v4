using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSignInCleanupRepository.cs
//
// PURPOSE:
// Deletes expired sign-in rows in batches.
//
// RESPONSIBILITIES:
// - platform.UserSessions
// - platform.SignInChallenges, platform.PasskeyCeremonies,
//   platform.AccountTokens, platform.RegistrationTokens and
//   platform.DeviceChallenges
//
// IMPORTANT NOTES:
// - Set-based deletes; nothing references these rows
// - Device links are kept: they carry support history
// =========================================================
internal sealed class PlatformSignInCleanupRepository : IPlatformSignInCleanupRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSignInCleanupRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> RemoveSessionsExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken) =>
        DeleteAsync(_context.UserSessions.Where(item => item.ExpiresUtc < cutoffUtc), batchSize, cancellationToken);

    public async Task<int> RemoveLinksExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken)
    {
        int removed = await DeleteAsync(_context.SignInChallenges.Where(item => item.ExpiresUtc < cutoffUtc), batchSize, cancellationToken);
        removed += await DeleteAsync(_context.PasskeyCeremonies.Where(item => item.ExpiresUtc < cutoffUtc), batchSize, cancellationToken);
        removed += await DeleteAsync(_context.AccountTokens.Where(item => item.ExpiresUtc < cutoffUtc), batchSize, cancellationToken);
        removed += await DeleteAsync(_context.RegistrationTokens.Where(item => item.ExpiresUtc < cutoffUtc), batchSize, cancellationToken);
        return removed + await DeleteAsync(_context.DeviceChallenges.Where(item => item.ExpiresUtc < cutoffUtc), batchSize, cancellationToken);
    }

    private static async Task<int> DeleteAsync<T>(IQueryable<T> rows, int batchSize, CancellationToken cancellationToken)
    {
        int total = 0;
        int removed;

        do
        {
            removed = await rows.Take(batchSize).ExecuteDeleteAsync(cancellationToken);
            total += removed;
        }
        while (removed == batchSize);

        return total;
    }
}
