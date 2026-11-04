using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsParcelLabel;

// =========================================================
// FILE: PrintOperationsParcelLabelUseCase.cs
//
// PURPOSE:
// Prints a parcel's ID label again on a dock printer, or a test label (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A parcel label needs ops.parcels.view for the printer's warehouse; a test label needs ops.printers.manage
// =========================================================
public sealed class PrintOperationsParcelLabelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsPrintingRepository _printing;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsLabelPrinting _labels;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public PrintOperationsParcelLabelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsPrintingRepository printing,
        IOperationsTrackingRepository tracking,
        IOperationsCustomerRepository customers,
        OperationsLabelPrinting labels,
        IOperationsUnitOfWork unitOfWork)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _labels = labels ?? throw new ArgumentNullException(nameof(labels));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PrintOperationsParcelLabelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PrintOperationsParcelLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return PrintOperationsParcelLabelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _printing.GetPrinterAsync(request.PrinterId, cancellationToken) is not { IsActive: true } printer
            || await _scope.CoveredAsync(access, request.ParcelId is null ? OperationsPermissions.PrintersManage : OperationsPermissions.ParcelsView, printer.WarehouseId, cancellationToken) is null)
        {
            return PrintOperationsParcelLabelResponse.Failure(OperationsPrintingErrors.PrinterNotFound);
        }

        if (request.ParcelId is { } parcelId)
        {
            // An ID label only on a printer holding ID labels (review of 4D step 2).
            if (printer.Media != OperationsLabelMedia.IdLabel)
            {
                return PrintOperationsParcelLabelResponse.Failure(OperationsPrintingErrors.PrinterNotFound);
            }

            if (await _tracking.GetParcelAsync(parcelId, cancellationToken) is not { Number: { } number } parcel || parcel.WarehouseId != printer.WarehouseId)
            {
                return PrintOperationsParcelLabelResponse.Failure(Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.ParcelNotFound);
            }

            string? code = parcel.CustomerId is { } customerId ? (await _customers.GetAsync(customerId, cancellationToken))?.Code : null;
            _labels.QueueParcelLabel(context.RequiredTenantId, printer, number, code);
        }
        else
        {
            // Slice 4D step 2: a shipping label printer gets a sample shipping label.
            if (printer.Media == OperationsLabelMedia.ShippingLabel)
            {
                _labels.QueueShippingLabel(context.RequiredTenantId, printer,
                    new OperationsShippingLabel("TEST", "TEST0000", "TEST0000", 1, 1, "AAA", "BBB", null, "TEST", "TEST", true, "1 kg"));
            }
            else
            {
                _labels.QueueParcelLabel(context.RequiredTenantId, printer, "TEST0000", "TEST");
            }
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PrintOperationsParcelLabelResponse.Success(printer.Id)
            : PrintOperationsParcelLabelResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
