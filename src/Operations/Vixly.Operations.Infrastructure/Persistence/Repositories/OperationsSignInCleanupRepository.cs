using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsSignInCleanupRepository.cs
//
// PURPOSE:
// Deletes expired sign-in rows in batches.
//
// RESPONSIBILITIES:
// - ops.UserSessions and ops.CustomerSessions
// - ops.SignInChallenges, ops.AccountTokens and
//   ops.CustomerTokens
//
// IMPORTANT NOTES:
// - Set-based deletes; nothing references these rows
// =========================================================
internal sealed class OperationsSignInCleanupRepository : IOperationsSignInCleanupRepository
{
    private readonly OperationsDbContext _context;

    public OperationsSignInCleanupRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> RemoveSessionsExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken)
    {
        int removed = await DeleteAsync(_context.UserSessions.Where(item => item.ExpiresUtc < cutoffUtc).OrderBy(item => item.ExpiresUtc), batchSize, cancellationToken);
        return removed + await DeleteAsync(_context.CustomerSessions.Where(item => item.ExpiresUtc < cutoffUtc).OrderBy(item => item.ExpiresUtc), batchSize, cancellationToken);
    }

    public async Task<int> RemoveLinksExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken)
    {
        int removed = await DeleteAsync(_context.SignInChallenges.Where(item => item.ExpiresUtc < cutoffUtc).OrderBy(item => item.ExpiresUtc), batchSize, cancellationToken);
        removed += await DeleteAsync(_context.AccountTokens.Where(item => item.ExpiresUtc < cutoffUtc).OrderBy(item => item.ExpiresUtc), batchSize, cancellationToken);
        return removed + await DeleteAsync(_context.CustomerTokens.Where(item => item.ExpiresUtc < cutoffUtc).OrderBy(item => item.ExpiresUtc), batchSize, cancellationToken);
    }

    // Oldest first, one batch at a time.
    private static async Task<int> DeleteAsync<T>(IOrderedQueryable<T> rows, int batchSize, CancellationToken cancellationToken)
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
