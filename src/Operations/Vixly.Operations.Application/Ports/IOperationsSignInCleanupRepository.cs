namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSignInCleanupRepository.cs
//
// PURPOSE:
// Removes sign-in rows nobody needs any more, across every
// company (background jobs comparison, 2026-10-22).
//
// RESPONSIBILITIES:
// - Staff and customer sessions that expired before a date
// - Sign-in challenges, invitation and password links, customer
//   links that expired before another date
//
// IMPORTANT NOTES:
// - Runs in the system scope; deletes in batches so a large
//   backlog never holds a long lock
// =========================================================
public interface IOperationsSignInCleanupRepository
{
    Task<int> RemoveSessionsExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken);

    Task<int> RemoveLinksExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken);
}
