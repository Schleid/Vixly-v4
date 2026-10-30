using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsLocationRepository.cs
//
// PURPOSE:
// Branches, warehouses and country contacts of the current tenant (Slice 1B).
//
// RESPONSIBILITIES:
// - Add, load and list them
// - Tell whether a code is taken
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsLocationRepository
{
    void Add(OperationsBranch branch);

    void Add(OperationsWarehouse warehouse);

    void Add(OperationsCountryContact contact);

    void Remove(OperationsCountryContact contact);

    Task<OperationsBranch?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken);

    Task<OperationsWarehouse?> GetWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken);

    Task<OperationsCountryContact?> GetContactAsync(Guid contactId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsBranch>> ListBranchesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsWarehouse>> ListWarehousesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCountryContact>> ListContactsAsync(CancellationToken cancellationToken);

    Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken cancellationToken);
}
