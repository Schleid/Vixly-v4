using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsUnitOfWork.cs
//
// PURPOSE:
// Commits a use case changes in one SaveChanges (D-2).
//
// RESPONSIBILITIES:
// - Save every tracked change together
// - Turn a concurrency conflict or a unique-index clash into false
//
// IMPORTANT NOTES:
// - A commit that has started always finishes: the request token only stops work before it starts (ADR-0019)
// - Other database errors propagate; the host logs them with the correlation id (S-9)
// =========================================================
internal sealed class OperationsUnitOfWork : IOperationsUnitOfWork
{
    private readonly OperationsDbContext _context;

    public OperationsUnitOfWork(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _context.SaveChangesAsync(CancellationToken.None);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<bool> SaveInTwoStepsAsync(Action secondStep, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(secondStep);

        // The retrying execution strategy needs the whole transaction as one unit it can repeat.
        return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            if (!await SaveChangesAsync(cancellationToken))
            {
                return false;
            }

            secondStep();

            if (!await SaveChangesAsync(cancellationToken))
            {
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public void DiscardChanges()
    {
        _context.ChangeTracker.Clear();
    }
}
