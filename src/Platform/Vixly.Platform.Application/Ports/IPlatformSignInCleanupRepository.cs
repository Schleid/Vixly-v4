namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSignInCleanupRepository.cs
//
// PURPOSE:
// Removes Control and sign-up rows nobody needs any more
// (background jobs comparison, 2026-10-22).
//
// RESPONSIBILITIES:
// - Staff sessions that expired before a date
// - Sign-in challenges, passkey ceremonies, account links,
//   registration links and device challenges that expired
//   before another date
//
// IMPORTANT NOTES:
// - Deletes in batches so a large backlog never holds a long
//   lock
// =========================================================
public interface IPlatformSignInCleanupRepository
{
    Task<int> RemoveSessionsExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken);

    Task<int> RemoveLinksExpiredBeforeAsync(DateTimeOffset cutoffUtc, int batchSize, CancellationToken cancellationToken);
}
