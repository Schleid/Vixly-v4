namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsUnitOfWork.cs
//
// PURPOSE:
// Commits a use case's changes in one save (D-2).
//
// RESPONSIBILITIES:
// - Save every tracked change together
// - Report a concurrency conflict or a unique-index clash as false
//
// IMPORTANT NOTES:
// - A started commit always finishes, whatever the request does (ADR-0019)
// =========================================================
public interface IOperationsUnitOfWork
{
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);

    void DiscardChanges();

    // Saves, applies the second step, saves again: both saves commit together or neither does.
    // Used where a unique index forbids doing both in one statement (ownership transfer).
    async Task<bool> SaveInTwoStepsAsync(Action secondStep, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(secondStep);

        if (!await SaveChangesAsync(cancellationToken))
        {
            return false;
        }

        secondStep();
        return await SaveChangesAsync(cancellationToken);
    }
}
