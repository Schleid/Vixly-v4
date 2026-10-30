using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ListOperationsInspectionQueue;

// =========================================================
// FILE: ListOperationsInspectionQueueUseCase.cs
//
// PURPOSE:
// The inspection queue of the warehouses a person covers, oldest first (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Waiting and in progress; a disposal awaiting approval is on the holds page
// =========================================================
public sealed class ListOperationsInspectionQueueUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsInspectionReader _reader;

    public ListOperationsInspectionQueueUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsInspectionRepository inspections,
        OperationsInspectionReader reader)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<ListOperationsInspectionQueueResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsInspectionQueueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsInspectionQueueResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // Scoped by place: a warehouse grant shows that warehouse's queue only.
        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.InspectionPerform, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsInspectionQueueResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        (IReadOnlyList<OperationsInspection> items, int total) = await _inspections.ListQueueAsync(shown, Math.Max(request.Page, 1), PageSize, cancellationToken, request.ParcelId);
        return ListOperationsInspectionQueueResponse.Success(await _reader.WithParcelsAsync(items, cancellationToken), total, warehouses);
    }

    public const int PageSize = 25;
    
}
