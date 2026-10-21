using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsReceivingScope.cs
//
// PURPOSE:
// Which warehouses a person may work in for receiving and parcels (Slice 3B).
//
// RESPONSIBILITIES:
// - List the active warehouses the viewer's grants cover for a permission
// - Say whether one warehouse is covered
//
// IMPORTANT NOTES:
// - Unlike company matters, a warehouse or branch grant counts here (design section 8)
// =========================================================
public sealed class OperationsReceivingScope
{
    private readonly IOperationsLocationRepository _locations;

    public OperationsReceivingScope(IOperationsLocationRepository locations)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<IReadOnlyList<OperationsWarehouse>> WarehousesAsync(OperationsAccessView access, string permission, bool receivingOnly, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        return (await _locations.ListWarehousesAsync(cancellationToken))
            .Where(warehouse => warehouse.Status == OperationsLocationStatus.Active
                && (!receivingOnly || warehouse.Roles.HasFlag(OperationsWarehouseRoles.Receiving))
                && access.CoversWarehouse(permission, warehouse.Id, warehouse.BranchId))
            .OrderBy(warehouse => warehouse.Code, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<OperationsWarehouse?> CoveredAsync(OperationsAccessView access, string permission, Guid? warehouseId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        return warehouseId is { } id && await _locations.GetWarehouseAsync(id, cancellationToken) is { } warehouse
            && access.CoversWarehouse(permission, warehouse.Id, warehouse.BranchId)
                ? warehouse
                : null;
    }
}
