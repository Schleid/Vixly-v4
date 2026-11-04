using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.UseCases.Printing.ListOperationsLabelsToPrint;

// =========================================================
// FILE: ListOperationsLabelsToPrintUseCase.cs
//
// PURPOSE:
// The cleared parcels whose shipping label was never printed, per warehouse, with the printers holding shipping labels (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - An empty warehouse choice shows every warehouse the person may see
// =========================================================
public sealed class ListOperationsLabelsToPrintUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsPrintingRepository _printing;

    private readonly OperationsParcelReader _reader;

    public ListOperationsLabelsToPrintUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsPrintingRepository printing,
        OperationsParcelReader reader)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<ListOperationsLabelsToPrintResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsLabelsToPrintRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsLabelsToPrintResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsView, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsLabelsToPrintResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        List<OperationsLabelPrinter> printers = new();

        foreach (Guid warehouseId in shown)
        {
            printers.AddRange((await _printing.ListPrintersAsync(warehouseId, cancellationToken)).Where(printer => printer.IsActive && printer.Media == OperationsLabelMedia.ShippingLabel));
        }

        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = await _receiving.ListLabelsToPrintAsync(shown, MaxShown, cancellationToken);
        return ListOperationsLabelsToPrintResponse.Success(await _reader.SummariesAsync(parcels, cancellationToken), printers, warehouses);
    }

    public const int MaxShown = 100;
}
