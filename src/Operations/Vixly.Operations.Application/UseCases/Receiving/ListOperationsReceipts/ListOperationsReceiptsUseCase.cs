using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsReceipts;

// =========================================================
// FILE: ListOperationsReceiptsUseCase.cs
//
// PURPOSE:
// The carrier deliveries of the warehouses a person receives at: open ones, then receipts (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.receiving.receive
// =========================================================
public sealed class ListOperationsReceiptsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    public ListOperationsReceiptsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public async Task<ListOperationsReceiptsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsReceiptsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsReceiptsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ReceivingReceive, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsReceiptsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> ids = warehouses.Select(warehouse => warehouse.Id).ToList();
        IReadOnlyList<OperationsDeliverySummary> open = (await _receiving.ListOpenDeliveriesAsync(ids, cancellationToken))
            .Select(item => new OperationsDeliverySummary(item.Delivery.Id, item.Delivery.WarehouseId, item.Delivery.Carrier, item.Delivery.CreatedUtc, item.Pieces))
            .ToList();
        (IReadOnlyList<OperationsWarehouseReceipt> receipts, int total) = await _receiving.ListReceiptsAsync(ids, Math.Max(request.Page, 1), PageSize, cancellationToken);

        return ListOperationsReceiptsResponse.Success(open, receipts, total, warehouses);
    }

    public const int PageSize = 25;
    
}
