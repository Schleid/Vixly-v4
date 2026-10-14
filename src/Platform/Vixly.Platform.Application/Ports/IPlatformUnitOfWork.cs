namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformUnitOfWork.cs
//
// PURPOSE:
// Commits every change a use case made, in one transaction.
//
// RESPONSIBILITIES:
// - Save all tracked changes together (D-2)
// - Report an optimistic-concurrency conflict as a result,
//   not an exception
// - Forget every loaded record between two operations of a
//   long-lived screen, so the next use case reads fresh data
//
// IMPORTANT NOTES:
// - Returns false when another user changed the same record
//   first; the use case answers ConcurrencyConflict
// =========================================================
public interface IPlatformUnitOfWork
{
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);

    void DiscardChanges();
}
