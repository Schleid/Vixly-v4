using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsShippingLabel;

// =========================================================
// FILE: PrintOperationsShippingLabelUseCase.cs
//
// PURPOSE:
// Prints the shipping label of a cleared parcel on a printer holding 4 x 6 labels (Slice 4D step 2, A-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.view for the parcel's warehouse; the printer must be of that warehouse and hold shipping labels
// =========================================================
public sealed class PrintOperationsShippingLabelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsLabelPrinting _labels;

    private readonly OperationsShippingLabels _shippingLabels;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PrintOperationsShippingLabelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTrackingRepository tracking,
        OperationsLabelPrinting labels,
        OperationsShippingLabels shippingLabels,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _labels = labels ?? throw new ArgumentNullException(nameof(labels));
        _shippingLabels = shippingLabels ?? throw new ArgumentNullException(nameof(shippingLabels));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PrintOperationsShippingLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PrintOperationsShippingLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return PrintOperationsShippingLabelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is null)
        {
            return PrintOperationsShippingLabelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (request.OnlyIfNotPrinted && parcel.ShippingLabelUtc is not null)
        {
            return PrintOperationsShippingLabelResponse.Success(parcel.Id, true);
        }

        if (await _labels.UsableAsync(request.PrinterId, warehouseId, OperationsLabelMedia.ShippingLabel, cancellationToken) is not { } printer)
        {
            return PrintOperationsShippingLabelResponse.Failure(OperationsPrintingErrors.PrinterNotFound);
        }

        if (await _shippingLabels.BuildAsync(parcel, cancellationToken) is not { } label)
        {
            return PrintOperationsShippingLabelResponse.Failure(OperationsPrintingErrors.NotCleared);
        }

        _labels.QueueShippingLabel(context.RequiredTenantId, printer, label);
        parcel.ShippingLabelPrinted(_timeProvider.GetUtcNow());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PrintOperationsShippingLabelResponse.Success(parcel.Id)
            : PrintOperationsShippingLabelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
