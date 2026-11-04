using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsShelfCountRepository.cs
//
// PURPOSE:
// Shelf counts (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Add a count; get one (tracked); the open count of a shelf; the latest counts of warehouses
//
// IMPORTANT NOTES:
// - Scoped to the tenant by the data layer
// =========================================================
public interface IOperationsShelfCountRepository
{
    void Add(OperationsShelfCount count);

    Task<OperationsShelfCount?> GetAsync(Guid countId, CancellationToken cancellationToken);

    Task<OperationsShelfCount?> FindOpenAsync(Guid locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsShelfCount>> ListAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);
}
