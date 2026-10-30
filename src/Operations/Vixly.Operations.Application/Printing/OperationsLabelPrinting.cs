using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Printing;

namespace Vixly.Operations.Application.Printing;

// =========================================================
// FILE: OperationsLabelPrinting.cs
//
// PURPOSE:
// Sends a parcel ID label to a dock printer (Slice 3B).
//
// RESPONSIBILITIES:
// - Check the printer belongs to the warehouse and is in use, then queue the label for the print program
//
// IMPORTANT NOTES:
// - Queued in the same unit of work as the scan, so a label exists exactly when the parcel does
// =========================================================
public sealed class OperationsLabelPrinting
{
    private readonly IOperationsPrintingRepository _printing;

    private readonly TimeProvider _timeProvider;

    public OperationsLabelPrinting(IOperationsPrintingRepository printing, TimeProvider timeProvider)
    {
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // Null when the printer is missing, stopped or in another warehouse.
    public async Task<OperationsLabelPrinter?> UsableAsync(Guid? printerId, Guid warehouseId, CancellationToken cancellationToken) =>
        printerId is { } id && await _printing.GetPrinterAsync(id, cancellationToken) is { IsActive: true } printer && printer.WarehouseId == warehouseId ? printer : null;

    // Slice 4D step 2: the shipping label on a printer holding 4 x 6 labels.
    public void QueueShippingLabel(Guid tenantId, OperationsLabelPrinter printer, Vixly.Operations.Application.Ports.OperationsShippingLabel label)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printing.Add(OperationsPrintJob.Queue(tenantId, printer, OperationsZplLabels.ShippingLabel(label, printer.Dpi), _timeProvider.GetUtcNow()));
    }

    // Null when the printer is missing, stopped, in another warehouse, or holds other labels.
    public async Task<OperationsLabelPrinter?> UsableAsync(Guid? printerId, Guid warehouseId, OperationsLabelMedia media, CancellationToken cancellationToken) =>
        await UsableAsync(printerId, warehouseId, cancellationToken) is { } printer && printer.Media == media ? printer : null;

    public void QueueParcelLabel(Guid tenantId, OperationsLabelPrinter printer, string parcelNumber, string? customerCode)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printing.Add(OperationsPrintJob.Queue(tenantId, printer, OperationsZplLabels.ParcelLabel(parcelNumber, customerCode, printer.Dpi), _timeProvider.GetUtcNow()));
    }
}
