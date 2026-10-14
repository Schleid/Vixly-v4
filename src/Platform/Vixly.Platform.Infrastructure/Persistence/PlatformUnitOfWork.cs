using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Persistence;

// =========================================================
// FILE: PlatformUnitOfWork.cs
//
// PURPOSE:
// Commits a use case's changes in one SaveChanges.
//
// RESPONSIBILITIES:
// - Save every tracked change together (D-2)
// - Turn an optimistic-concurrency conflict into false
//
// IMPORTANT NOTES:
// - A unique-index violation (for example two claims racing for
//   one portal slug) also returns false, after clearing the
//   tracked changes
// - Other database errors propagate; the host logs them with
//   the correlation id and shows a generic error (S-9)
// =========================================================
internal sealed class PlatformUnitOfWork : IPlatformUnitOfWork
{
    private readonly PlatformDbContext _context;

    public PlatformUnitOfWork(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }

    public void DiscardChanges()
    {
        _context.ChangeTracker.Clear();
    }
}
