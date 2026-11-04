using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.BillOperationsDelivery;

// =========================================================
// FILE: BillOperationsDeliveryUseCase.cs
//
// PURPOSE:
// Bill a shipment's destination charges before it goes out for delivery (Slice 7 step 3, R-4, R-9, R-13).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at the warehouse; payment is taken by the cashier, or at the door with cash on delivery (R-13)
// - Only a shipment waiting for delivery there
// =========================================================
public sealed class BillOperationsDeliveryUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly OperationsInvoicing _invoicing;

    private readonly IOperationsDeliveryRepository _routes;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public BillOperationsDeliveryUseCase(
        OperationsDeliveryDesk desk,
        OperationsInvoicing invoicing,
        IOperationsDeliveryRepository routes,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _invoicing = invoicing ?? throw new ArgumentNullException(nameof(invoicing));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<BillOperationsDeliveryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        BillOperationsDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.DeliveryPlan, cancellationToken);

        if (refused is not null)
        {
            return BillOperationsDeliveryResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here)
        {
            return BillOperationsDeliveryResponse.Failure(OperationsDeliveryErrors.NotFound);
        }

        // Slice 7 step 4 (R-8): a partner agent bills only where its partner handles money.
        if (!access!.HandlesMoneyAt(here))
        {
            return BillOperationsDeliveryResponse.Failure(Vixly.Operations.Domain.Pickup.OperationsPickupErrors.MoneyNotHandled);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        if ((await _desk.WaitingAsync(here, settings, cancellationToken)).FirstOrDefault(item => item.Shipment.Id == request.ShipmentId) is not { Shipment: { } found }
            || (await _routes.ListShipmentsAsync(new[] { found.Id }, cancellationToken)).FirstOrDefault() is not { } shipment)
        {
            return BillOperationsDeliveryResponse.Failure(OperationsDeliveryErrors.NotFound);
        }

        // A double click finds the bill the first click made.
        if (request.RequestKey is { } key && shipment.DestinationBillKey == key
            && (await _desk.InvoicesAsync(shipment.Id, cancellationToken)).Where(invoice => invoice.Kind == OperationsInvoiceKind.Destination).MaxBy(invoice => invoice.CreatedUtc) is { } billed)
        {
            return BillOperationsDeliveryResponse.Success(billed.Id);
        }

        decimal?[] charges = { request.Duties, request.LocalTax, request.LocalFee };

        if (!settings.DestinationCharges || charges.Any(amount => amount is { } value && (value < 0m || value > 100_000m || decimal.Round(value, 2) != value)))
        {
            return BillOperationsDeliveryResponse.Failure(OperationsPickupErrors.AmountInvalid);
        }

        // Duties and local taxes are the government's: not taxed again; the local fee is the company's (R-4).
        List<OperationsInvoiceLine> lines = new();

        void Line(OperationsChargeLineKind kind, decimal? amount, bool taxed)
        {
            if (amount is > 0m)
            {
                lines.Add(new OperationsInvoiceLine(kind, null, null, null, null, 1m, null, amount.Value, amount.Value, 0m, 0m, OperationsTaxKind.Charges, taxed));
            }
        }

        Line(OperationsChargeLineKind.Duties, request.Duties, false);
        Line(OperationsChargeLineKind.LocalTax, request.LocalTax, false);
        Line(OperationsChargeLineKind.LocalFee, request.LocalFee, true);

        if (lines.Count == 0)
        {
            return BillOperationsDeliveryResponse.Failure(OperationsPickupErrors.NothingToBill);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Vixly.Operations.Domain.Billing.OperationsInvoice? invoice;

        try
        {
            invoice = await _invoicing.IssueForDestinationAsync(shipment, here, lines, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return BillOperationsDeliveryResponse.Failure(exception.Error);
        }

        if (invoice is null)
        {
            return BillOperationsDeliveryResponse.Failure(OperationsDeliveryErrors.NotFound);
        }

        shipment.BilledAtDestination(request.RequestKey, utcNow);
        _audit.Record(access!.User.Id, "ops.delivery.billed", nameof(OperationsShipment), shipment.Id, after: new { invoice = invoice.Number, request.Duties, request.LocalTax, request.LocalFee });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return BillOperationsDeliveryResponse.Success(invoice.Id);
        }

        _unitOfWork.DiscardChanges();
        return BillOperationsDeliveryResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
