using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsStockCountRepository.cs
//
// PURPOSE:
// Stores the counts of the undeclared parcels area (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Add, read and list counts; the undeclared parcels of a warehouse a count expects
//
// IMPORTANT NOTES:
// - Scoped to the current tenant by the query filter
// =========================================================
public interface IOperationsStockCountRepository
{
    void Add(OperationsStockCount count);

    Task<OperationsStockCount?> GetAsync(Guid countId, CancellationToken cancellationToken);

    Task<OperationsStockCount?> FindOpenAsync(Guid warehouseId, CancellationToken cancellationToken);

    // Newest first.
    Task<IReadOnlyList<OperationsStockCount>> ListAsync(IReadOnlyCollection<Guid> warehouses, int take, CancellationToken cancellationToken);

    // Packages nobody could place, still at the warehouse.
    Task<IReadOnlyList<OperationsParcel>> ListUndeclaredAsync(Guid warehouseId, int take, CancellationToken cancellationToken);
}
