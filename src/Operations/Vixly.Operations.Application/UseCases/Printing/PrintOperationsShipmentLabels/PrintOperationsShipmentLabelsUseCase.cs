using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsShipmentLabels;

// =========================================================
// FILE: PrintOperationsShipmentLabelsUseCase.cs
//
// PURPOSE:
// Prints all the labels of one shipment at once, instead of one parcel at a time (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - ID labels: every parcel of the shipment, in one PDF or on one ID-label printer
// - Shipping labels: every cleared parcel, in one PDF or on one shipping-label printer, each marked printed; the others are counted as waiting
//
// IMPORTANT NOTES:
// - ID labels: staff who see the warehouse's parcels, or the counter staff of a shipment made at the counter (the same rule as one label)
// - Shipping labels: staff who see the warehouse's parcels; a parcel still held or not inspected never gets one (A-5)
// - Warehouse reception keeps printing one ID label per scan
// =========================================================
public sealed class PrintOperationsShipmentLabelsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly OperationsLabelPrinting _labels;

    private readonly OperationsShippingLabels _shippingLabels;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PrintOperationsShipmentLabelsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers,
        IOperationsBrandRepository brands,
        IOperationsLabelRenderer renderer,
        OperationsLabelPrinting labels,
        OperationsShippingLabels shippingLabels,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _labels = labels ?? throw new ArgumentNullException(nameof(labels));
        _shippingLabels = shippingLabels ?? throw new ArgumentNullException(nameof(shippingLabels));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PrintOperationsShipmentLabelsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PrintOperationsShipmentLabelsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.NotFound);
        }

        // Another warehouse's shipment answers like a missing one (S-3).
        OperationsWarehouse? warehouse = await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, shipment.OriginWarehouseId, cancellationToken);

        if (warehouse is null && !request.Shipping && shipment.CreatedAtCounter)
        {
            warehouse = await _scope.CoveredAsync(access, OperationsPermissions.CounterServe, shipment.OriginWarehouseId, cancellationToken);
        }

        if (warehouse is null)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.NotFound);
        }

        IReadOnlyList<OperationsParcel> parcels = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken))
            .Where(parcel => parcel.Number is not null).OrderBy(parcel => parcel.CreatedUtc).ThenBy(parcel => parcel.Number, StringComparer.Ordinal).ToList();

        if (parcels.Count == 0)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        string name = (shipment.Number ?? "shipment") + (request.Shipping ? "-shipping-labels.pdf" : "-labels.pdf");
        return request.Shipping
            ? await ShippingAsync(context.RequiredTenantId, warehouse, parcels, request.PrinterId, name, cancellationToken)
            : await IdLabelsAsync(context.RequiredTenantId, warehouse, parcels, request.PrinterId, name, cancellationToken);
    }

    private async Task<PrintOperationsShipmentLabelsResponse> IdLabelsAsync(Guid tenantId, OperationsWarehouse warehouse, IReadOnlyList<OperationsParcel> parcels, Guid? printerId,
        string name, CancellationToken cancellationToken)
    {
        string company = (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty;
        Dictionary<Guid, string?> codes = new();

        foreach (Guid customerId in parcels.Where(parcel => parcel.CustomerId is not null).Select(parcel => parcel.CustomerId!.Value).Distinct())
        {
            codes[customerId] = (await _customers.GetAsync(customerId, cancellationToken))?.Code;
        }

        string? Code(OperationsParcel parcel) => parcel.CustomerId is { } id ? codes.GetValueOrDefault(id) : null;

        if (printerId is null)
        {
            List<OperationsParcelLabel> labels = parcels.Select(parcel => new OperationsParcelLabel(parcel.Number!, Code(parcel), company, warehouse.Code, parcel.CreatedUtc)).ToList();
            return PrintOperationsShipmentLabelsResponse.Success(_renderer.RenderParcelLabels(labels), name, labels.Count, 0);
        }

        if (await _labels.UsableAsync(printerId, warehouse.Id, OperationsLabelMedia.IdLabel, cancellationToken) is not { } printer)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(OperationsPrintingErrors.PrinterNotFound);
        }

        foreach (OperationsParcel parcel in parcels)
        {
            _labels.QueueParcelLabel(tenantId, printer, parcel.Number!, Code(parcel));
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PrintOperationsShipmentLabelsResponse.Success(null, name, parcels.Count, 0)
            : PrintOperationsShipmentLabelsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private async Task<PrintOperationsShipmentLabelsResponse> ShippingAsync(Guid tenantId, OperationsWarehouse warehouse, IReadOnlyList<OperationsParcel> parcels, Guid? printerId,
        string name, CancellationToken cancellationToken)
    {
        OperationsLabelPrinter? printer = null;

        if (printerId is not null && (printer = await _labels.UsableAsync(printerId, warehouse.Id, OperationsLabelMedia.ShippingLabel, cancellationToken)) is null)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(OperationsPrintingErrors.PrinterNotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<OperationsShippingLabel> labels = new();

        foreach (OperationsParcel parcel in parcels)
        {
            // Not cleared (held, not inspected, not measured) or not paid: no label yet; it shows in the labels to print once it can.
            if (await _shippingLabels.BuildAsync(parcel, cancellationToken) is not { } label)
            {
                continue;
            }

            labels.Add(label);
            parcel.ShippingLabelPrinted(utcNow);

            if (printer is not null)
            {
                _labels.QueueShippingLabel(tenantId, printer, label);
            }
        }

        if (labels.Count == 0)
        {
            return PrintOperationsShipmentLabelsResponse.Failure(parcels.Count > 0 ? await _shippingLabels.RefusalAsync(parcels[0], cancellationToken) : OperationsPrintingErrors.NotCleared);
        }

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return PrintOperationsShipmentLabelsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return PrintOperationsShipmentLabelsResponse.Success(printer is null ? _renderer.RenderShippingLabels(labels) : null, name, labels.Count, parcels.Count - labels.Count);
    }
}
