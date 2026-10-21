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

namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickup;

// =========================================================
// FILE: GetOperationsPickupUseCase.cs
//
// PURPOSE:
// A shipment at the pickup counter: who may collect, what is ready, what is owed, the proof asked (Slice 7 step 2, R-4 to R-6, R-9, R-10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.pickup.release at the warehouse
// - A shipment with nothing here is not found
// =========================================================
public sealed class GetOperationsPickupUseCase
{
    private readonly OperationsPickupDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsPickupUseCase(
        OperationsPickupDesk desk,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsPickupResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPickupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsPickupResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here
            || await _desk.ShipmentAsync(request.ShipmentId, here, cancellationToken) is not { } view)
        {
            return GetOperationsPickupResponse.Failure(OperationsPickupErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsShipment shipment = view.Shipment;
        Vixly.Operations.Domain.Customers.OperationsCustomer? customer = await _customers.GetAsync(shipment.CustomerId, cancellationToken);
        decimal storage = settings.DestinationStorage ? view.StorageOwed(utcNow) : 0m;
        IReadOnlyDictionary<Guid, OperationsWarehouse> codes = warehouses.ToDictionary(item => item.Id);
        string currency = view.Invoices.FirstOrDefault()?.CurrencyCode ?? settings.CurrencyCode;
        return GetOperationsPickupResponse.Success(shipment.Id, shipment.Number ?? string.Empty, customer is null ? string.Empty : customer.FirstName + " " + customer.LastName, customer?.Phone,
            shipment.RecipientName, shipment.RecipientPhone, shipment.CollectorName, shipment.CollectorPhone,
            new OperationsArrivalPlace(here.Id, here.Code, here.Name, here.ClearsCustoms) { TimeZone = here.TimeZone },
            view.Parcels.Select(parcel => new OperationsPickupParcelRow(parcel.Id, parcel.Number ?? string.Empty, parcel.Milestone, parcel.WarehouseId == here.Id,
                OperationsPickupDesk.IsReady(parcel, here), parcel.Flags.HasFlag(OperationsParcelFlags.CustomsHeld), settings.DestinationStorage && OperationsPickupDesk.IsReady(parcel, here)
                    ? parcel.UnbilledStorage(utcNow) : 0m)).ToList(),
            view.Invoices.Select(invoice => new OperationsPickupInvoiceRow(invoice.Id, invoice.Number, invoice.Kind, invoice.Total, invoice.Owed, invoice.PaymentState, invoice.CurrencyCode)).ToList(),
            view.Releases.Select(release => new OperationsPickupReleaseRow(release.Id, release.Collector, release.CollectorName, release.IdKind, release.IdLast, release.SignatureKey is not null,
                release.PhotoKey is not null, release.CodeChecked, release.ParcelCount, release.CreatedUtc, codes.TryGetValue(release.WarehouseId, out OperationsWarehouse? at) ? at.Code : string.Empty))
                .ToList(),
            storage, currency, settings.PickupProofs, settings.DestinationCharges,
            view.Ready.Count > 0 && (settings.DestinationCharges || storage > 0m), shipment.PickupCodeProtected is not null,
            OperationsBundling.Unpaid(view.Invoices));
    }
}
