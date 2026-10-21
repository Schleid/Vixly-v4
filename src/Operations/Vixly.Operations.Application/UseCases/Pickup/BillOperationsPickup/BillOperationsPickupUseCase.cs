using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.BillOperationsPickup;

// =========================================================
// FILE: BillOperationsPickupUseCase.cs
//
// PURPOSE:
// Bill a shipment's destination charges and storage at the pickup counter (Slice 7 step 2, R-4, R-10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.pickup.release at the warehouse; payment is taken by the cashier (R-9)
// - Storage counted to the day it is billed
// - One destination invoice per shipment
// =========================================================
public sealed class BillOperationsPickupUseCase
{
    private readonly OperationsPickupDesk _desk;

    private readonly OperationsInvoicing _invoicing;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsPickupCodes _codes;

    public BillOperationsPickupUseCase(
        OperationsPickupDesk desk,
        OperationsInvoicing invoicing,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        OperationsPickupCodes codes)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _invoicing = invoicing ?? throw new ArgumentNullException(nameof(invoicing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<BillOperationsPickupResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        BillOperationsPickupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return BillOperationsPickupResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here
            || await _desk.ShipmentAsync(request.ShipmentId, here, cancellationToken) is not { } view)
        {
            return BillOperationsPickupResponse.Failure(OperationsPickupErrors.NotFound);
        }

        // A double click finds the bill the first click made (review); a later visit may bill again (new storage, new charges).
        if (request.RequestKey is { } key && view.Shipment.DestinationBillKey == key && view.DestinationInvoice is { } billed)
        {
            return BillOperationsPickupResponse.Success(billed.Id);
        }

        if (view.Ready.Count == 0)
        {
            return BillOperationsPickupResponse.Failure(OperationsPickupErrors.NothingReady);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        decimal?[] charges = { request.Duties, request.LocalTax, request.LocalFee };

        if (charges.Any(amount => amount is { } value && (value < 0m || value > 100_000m || decimal.Round(value, 2) != value))
            || (!settings.DestinationCharges && charges.Any(amount => amount is > 0m)))
        {
            return BillOperationsPickupResponse.Failure(OperationsPickupErrors.AmountInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        decimal storage = settings.DestinationStorage ? decimal.Round(view.StorageOwed(utcNow), 2) : 0m;
        List<OperationsInvoiceLine> lines = new();

        // Duties and local taxes are the government's: not taxed again; the local fee and storage are the company's (R-4).
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
        Line(OperationsChargeLineKind.Storage, storage, true);

        if (lines.Count == 0)
        {
            return BillOperationsPickupResponse.Failure(OperationsPickupErrors.NothingToBill);
        }

        Vixly.Operations.Domain.Billing.OperationsInvoice? invoice;

        try
        {
            invoice = await _invoicing.IssueForDestinationAsync(view.Shipment, here, lines, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return BillOperationsPickupResponse.Failure(exception.Error);
        }

        if (invoice is null)
        {
            return BillOperationsPickupResponse.Failure(OperationsPickupErrors.NotFound);
        }

        // The storage billed so far, per parcel: the next bill counts from here (review).
        if (storage > 0m)
        {
            foreach (OperationsParcel parcel in view.Ready)
            {
                parcel.StorageBilled(utcNow);
            }
        }

        view.Shipment.BilledAtDestination(request.RequestKey, utcNow);
        _audit.Record(access!.User.Id, "ops.pickup.billed", nameof(OperationsShipment), view.Shipment.Id, after: new { invoice = invoice.Number, storage, request.Duties, request.LocalTax, request.LocalFee });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return BillOperationsPickupResponse.Success(invoice.Id);
        }

        _unitOfWork.DiscardChanges();
        return BillOperationsPickupResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
