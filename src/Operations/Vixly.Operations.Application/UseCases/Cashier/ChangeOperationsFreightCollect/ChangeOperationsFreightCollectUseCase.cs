using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsFreightCollect;

// =========================================================
// FILE: ChangeOperationsFreightCollectUseCase.cs
//
// PURPOSE:
// Freight collect or paid here, chosen at the cashier or the counter (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Mark a shipment and its invoice "pay at the destination", or back to "paid here"
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Any person who takes payments (ops.payments.take) at the shipment's warehouse; the company must allow freight collect
// - A payment term, not an amount: the issued invoice keeps its lines, so the "never edited" rule holds
// - Only while nothing is paid, pending or on account; collect never with supplies; back to "paid here" only until a parcel left
// - Audited with the reason: who, when, which way
// =========================================================
public sealed class ChangeOperationsFreightCollectUseCase
{
    private const int ReasonMaxLength = 200;

    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsFreightCollectUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsReceivingRepository receiving,
        IOperationsSupplyRepository supplies,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsFreightCollectResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsFreightCollectRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsFreightCollectResponse.Failure(refused);
        }

        string? reason = request.Reason?.Trim();

        if (reason is { Length: > ReasonMaxLength } || reason?.Any(char.IsControl) == true)
        {
            return ChangeOperationsFreightCollectResponse.Failure(OperationsBillingErrors.TextInvalid);
        }

        // The cashier names the invoice; the counter names the shipment, which may already have one.
        OperationsInvoice? invoice = request.InvoiceId is { } invoiceId ? await _billing.GetInvoiceAsync(invoiceId, cancellationToken) : null;
        Guid? shipmentId = invoice?.ShipmentId ?? request.ShipmentId;

        if ((request.InvoiceId is not null && invoice is null) || shipmentId is not { } id || await _receiving.GetShipmentAsync(id, cancellationToken) is not { } shipment)
        {
            return ChangeOperationsFreightCollectResponse.Failure(request.InvoiceId is null ? OperationsShipmentErrors.NotFound : OperationsBillingErrors.InvoiceNotFound);
        }

        invoice ??= (await _billing.ListShipmentInvoicesAsync(new[] { shipment.Id }, cancellationToken))
            .FirstOrDefault(item => item.Kind == OperationsInvoiceKind.Shipment && item.Status == OperationsInvoiceStatus.Issued);
        Guid warehouseId = invoice?.WarehouseId ?? shipment.OriginWarehouseId;

        if (warehouses.FirstOrDefault(warehouse => warehouse.Id == warehouseId) is not { } warehouse)
        {
            return ChangeOperationsFreightCollectResponse.Failure(invoice is null ? OperationsShipmentErrors.NotFound : OperationsBillingErrors.InvoiceNotFound);
        }

        // Partner agents take payments only where their partner handles money (R-8).
        if (!access!.HandlesMoneyAt(warehouse))
        {
            return ChangeOperationsFreightCollectResponse.Failure(Vixly.Operations.Domain.Pickup.OperationsPickupErrors.MoneyNotHandled);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);

        if (request.Collect && !settings.AllowCollect)
        {
            return ChangeOperationsFreightCollectResponse.Failure(OperationsShipmentErrors.CollectUnavailable);
        }

        // Back to "paid here" only while every parcel is still at origin: one that left travels as collect.
        if (!request.Collect && (await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken))
            .Any(parcel => parcel.Milestone is >= OperationsMilestone.DepartedOrigin and < OperationsMilestone.Cancelled))
        {
            return ChangeOperationsFreightCollectResponse.Failure(OperationsBillingErrors.CollectDeparted);
        }

        // Supplies are paid at the counter, never at the destination.
        if (request.Collect && invoice is null && await _supplies.FindShipmentSaleAsync(shipment.Id, cancellationToken) is { Lines.Count: > 0 })
        {
            return ChangeOperationsFreightCollectResponse.Failure(OperationsBillingErrors.CollectWithSupplies);
        }

        bool changed;

        try
        {
            changed = invoice?.ChangePaymentPlace(request.Collect, utcNow) ?? false;
            changed = shipment.ChangeFreightCollect(request.Collect, utcNow) || changed;
        }
        catch (DomainException exception)
        {
            return ChangeOperationsFreightCollectResponse.Failure(exception.Error);
        }

        if (!changed)
        {
            return ChangeOperationsFreightCollectResponse.Success(false);
        }

        _audit.Record(access.User.Id, request.Collect ? "ops.shipments.freight_collect_on" : "ops.shipments.freight_collect_off", nameof(OperationsShipment), shipment.Id,
            after: new { invoice = invoice?.Number, reason });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsFreightCollectResponse.Success(true)
            : ChangeOperationsFreightCollectResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
