using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.FindOperationsParcelByScan;

// =========================================================
// FILE: FindOperationsParcelByScanUseCase.cs
//
// PURPOSE:
// Finds a parcel from a scan at a station: its ID label, or the carrier barcode (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another warehouse's parcel answers like a missing one (S-3)
// =========================================================
public sealed class FindOperationsParcelByScanUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    public FindOperationsParcelByScanUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public async Task<FindOperationsParcelByScanResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FindOperationsParcelByScanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return FindOperationsParcelByScanResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        string text = OperationsCarrierBarcode.Clean(request.Barcode);

        if (text.Length == 0)
        {
            return FindOperationsParcelByScanResponse.Failure(OperationsReceivingErrors.BarcodeUnreadable);
        }

        // The parcel's own ID label first; the carrier barcode works too, at the warehouses the person covers.
        if (await _receiving.FindParcelByNumberAsync(text, cancellationToken) is { WarehouseId: { } labelled } byNumber
            && await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, labelled, cancellationToken) is not null)
        {
            return FindOperationsParcelByScanResponse.Success(byNumber.Id);
        }

        List<Guid> warehouses = (await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsView, false, cancellationToken)).Select(warehouse => warehouse.Id).ToList();

        return await _receiving.FindActiveParcelAsync(warehouses, OperationsCarrierBarcode.Candidates(text), cancellationToken) is { } byTracking
            ? FindOperationsParcelByScanResponse.Success(byTracking.Id)
            : FindOperationsParcelByScanResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
    }
}
