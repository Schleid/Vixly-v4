using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.UseCases.Printing.GetOperationsShippingLabel;

// =========================================================
// FILE: GetOperationsShippingLabelUseCase.cs
//
// PURPOSE:
// The shipping label of a cleared parcel as a PDF, for a station that prints from the browser (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.view for the parcel's warehouse
// - A form post, not a link: it marks the label printed
// =========================================================
public sealed class GetOperationsShippingLabelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsShippingLabels _shippingLabels;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetOperationsShippingLabelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTrackingRepository tracking,
        OperationsShippingLabels shippingLabels,
        IOperationsLabelRenderer renderer,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _shippingLabels = shippingLabels ?? throw new ArgumentNullException(nameof(shippingLabels));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsShippingLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShippingLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsShippingLabelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is null)
        {
            return GetOperationsShippingLabelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (await _shippingLabels.BuildAsync(parcel, cancellationToken) is not { } label)
        {
            return GetOperationsShippingLabelResponse.Failure(OperationsPrintingErrors.NotCleared);
        }

        // Downloaded to print: it leaves the labels to print, and the shipment keeps its number.
        parcel.ShippingLabelPrinted(_timeProvider.GetUtcNow());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? GetOperationsShippingLabelResponse.Success(_renderer.RenderShippingLabel(label), label.ShipmentNumber + "-" + label.Piece + ".pdf")
            : GetOperationsShippingLabelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
