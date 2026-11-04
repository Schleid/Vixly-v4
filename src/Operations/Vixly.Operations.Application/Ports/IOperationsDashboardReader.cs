namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDashboardReader.cs
//
// PURPOSE:
// Counts what the staff home page and the menu show (Slice 11; menu counts 2026-11-04).
//
// RESPONSIBILITIES:
// - Count the asked counters, each over its own warehouses (none: the whole company)
//
// IMPORTANT NOTES:
// - Read only, tenant-filtered
// - Harmonization H-3 and H-4 (owner, 2026-11-04): every asked counter in one database command; a counter not asked is not counted
// - Keys: the home counters (OperationsDashboardCounters) and "reviews_new" (the menu); an unknown key counts 0
// =========================================================
public interface IOperationsDashboardReader
{
    Task<IReadOnlyDictionary<string, int>> CountManyAsync(IReadOnlyDictionary<string, IReadOnlyCollection<Guid>?> asks, DateOnly today, CancellationToken cancellationToken);
}
