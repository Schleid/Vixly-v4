namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDashboardReader.cs
//
// PURPOSE:
// Counts what the staff home page shows (Slice 11).
//
// RESPONSIBILITIES:
// - One count per counter, over the warehouses given (none: the whole company)
//
// IMPORTANT NOTES:
// - Read only, tenant-filtered; each a single COUNT in the database
// =========================================================
public interface IOperationsDashboardReader
{
    Task<int> CountAsync(string key, IReadOnlyCollection<Guid>? warehouses, DateOnly today, CancellationToken cancellationToken);
}
