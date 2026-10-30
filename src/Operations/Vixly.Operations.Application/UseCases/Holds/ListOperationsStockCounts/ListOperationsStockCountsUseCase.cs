using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Holds.ListOperationsStockCounts;

// =========================================================
// FILE: ListOperationsStockCountsUseCase.cs
//
// PURPOSE:
// The counts of the undeclared parcels area of the warehouses the staff member may see, newest first (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign
// =========================================================
public sealed class ListOperationsStockCountsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsStockCountRepository _counts;

    public ListOperationsStockCountsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsStockCountRepository counts)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
    }

    public async Task<ListOperationsStockCountsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsStockCountsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsStockCountsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsAssign, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsStockCountsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        return ListOperationsStockCountsResponse.Success(await _counts.ListAsync(shown, 24, cancellationToken), warehouses);
    }
}
