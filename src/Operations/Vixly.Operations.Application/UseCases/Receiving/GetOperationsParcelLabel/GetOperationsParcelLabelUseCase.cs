using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelLabel;

// =========================================================
// FILE: GetOperationsParcelLabelUseCase.cs
//
// PURPOSE:
// The ID barcode label of a parcel, as a PDF (Slice 3B, D-R3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Slice 5C: the counter clerk prints the label of the parcels they enter (ops.counter.serve at the warehouse)
// - The label carries the parcel number as a Code 128 barcode; the shipping label comes after inspection (section 11, point 3)
// =========================================================
public sealed class GetOperationsParcelLabelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsLabelRenderer _renderer;

    public GetOperationsParcelLabelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsCustomerRepository customers,
        IOperationsBrandRepository brands,
        IOperationsLabelRenderer renderer)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<GetOperationsParcelLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsParcelLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsParcelLabelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId, Number: { } number } parcel)
        {
            return GetOperationsParcelLabelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        // Slice 5C (review): the counter right prints only the labels of parcels entered at a counter, never the dock's.
        OperationsWarehouse? warehouse = await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken);

        if (warehouse is null && (await _tracking.ListForSubjectAsync(Vixly.Operations.Domain.Tracking.OperationsTrackingSubject.Parcel, parcel.Id, cancellationToken))
            .Any(item => item.Code == Vixly.Operations.Domain.Tracking.OperationsTrackingCodes.ReceivedAtOrigin && item.Source == Vixly.Operations.Domain.Tracking.OperationsTrackingSource.Counter))
        {
            warehouse = await _scope.CoveredAsync(access, OperationsPermissions.CounterServe, warehouseId, cancellationToken);
        }

        if (warehouse is null)
        {
            return GetOperationsParcelLabelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        string? customerCode = parcel.CustomerId is { } customerId ? (await _customers.GetAsync(customerId, cancellationToken))?.Code : null;
        string company = (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty;
        byte[] content = _renderer.RenderParcelLabel(new OperationsParcelLabel(number, customerCode, company, warehouse.Code, parcel.CreatedUtc));

        return GetOperationsParcelLabelResponse.Success(content, number + ".pdf");
    }
}
