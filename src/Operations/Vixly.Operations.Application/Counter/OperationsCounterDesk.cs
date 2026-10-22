using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Counter;

// =========================================================
// FILE: OperationsCounterDesk.cs
//
// PURPOSE:
// What every front-desk step shares (Slice 5C): who may serve at the counter, and where.
//
// RESPONSIBILITIES:
// - Check the clerk may serve customers at the counter, and list the warehouses where they do
// - List a customer's shipments for the counter, with their parcels against the count
//
// IMPORTANT NOTES:
// - ops.counter.serve is given per warehouse; a warehouse counts only when it receives packages
// - The clerk sees all the customer's shipments (owner, 2026-10-20); parcels are added only where the clerk serves
// =========================================================
public sealed class OperationsCounterDesk
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsLocationRepository _locations;

    public OperationsCounterDesk(OperationsOwnerCheck owner, OperationsReceivingScope scope, IOperationsReceivingRepository receiving, IOperationsLocationRepository locations)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    // The clerk and the warehouses they serve at; a refusal when they may not serve anywhere.
    public async Task<(OperationsAccessView? Access, IReadOnlyList<OperationsWarehouse> Counters, Error? Refused)> OpenAsync(OperationsAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return (null, Array.Empty<OperationsWarehouse>(), OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> counters = await _scope.WarehousesAsync(access, OperationsPermissions.CounterServe, true, cancellationToken);
        return counters.Count == 0 ? (null, counters, OperationsAccessErrors.PermissionRequired) : (access, counters, null);
    }

    public async Task<OperationsCounterShipment> RowAsync(OperationsShipment shipment, IReadOnlyList<OperationsWarehouse> counters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        IReadOnlyDictionary<Guid, int> entered = await _receiving.CountShipmentParcelsAsync(new[] { shipment.Id }, cancellationToken);
        return Row(shipment, (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(warehouse => warehouse.Id), entered, counters.Select(counter => counter.Id).ToHashSet());
    }

    public async Task<IReadOnlyList<OperationsCounterShipment>> ShipmentsAsync(Guid customerId, IReadOnlyList<OperationsWarehouse> counters, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsShipment> shipments = await _receiving.ListCustomerShipmentsAsync(customerId, cancellationToken);
        IReadOnlyDictionary<Guid, int> entered = await _receiving.CountShipmentParcelsAsync(shipments.Select(shipment => shipment.Id).ToList(), cancellationToken);
        Dictionary<Guid, OperationsWarehouse> warehouses = (await _locations.ListWarehousesAsync(cancellationToken)).ToDictionary(warehouse => warehouse.Id);
        HashSet<Guid> served = counters.Select(counter => counter.Id).ToHashSet();
        return shipments.Select(shipment => Row(shipment, warehouses, entered, served)).ToList();
    }

    // Where the customer stands, from their account and their shipments (owner, 2026-10-20).
    public async Task<OperationsCounterStanding> StandingAsync(Vixly.Operations.Domain.Customers.OperationsCustomer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);
        IReadOnlyList<OperationsShipment> shipments = await _receiving.ListCustomerShipmentsAsync(customer.Id, cancellationToken);
        string? reason = OperationsCounterStanding.ReasonFor(customer.Status);
        return new OperationsCounterStanding(reason is null, reason, customer.CreatedUtc,
            shipments.Count(shipment => shipment.Status == OperationsShipmentStatus.Open && shipment.HasInstructions),
            shipments.Count(shipment => shipment.Status == OperationsShipmentStatus.Booked),
            await _receiving.CountCustomerParcelsOnHoldAsync(customer.Id, cancellationToken),
            shipments.Count == 0 ? null : shipments.Max(shipment => shipment.CreatedUtc));
    }

    private static OperationsCounterShipment Row(OperationsShipment shipment, Dictionary<Guid, OperationsWarehouse> warehouses, IReadOnlyDictionary<Guid, int> entered,
        HashSet<Guid> served) =>
        new(
            shipment.Id,
            shipment.Number,
            shipment.CreatedUtc,
            warehouses.TryGetValue(shipment.OriginWarehouseId, out OperationsWarehouse? origin) ? origin.Name : null,
            shipment.DestinationWarehouseId is { } destination && warehouses.TryGetValue(destination, out OperationsWarehouse? to) ? to.Name : shipment.DestinationCountryCode,
            shipment.DeliveryAddress is not null,
            shipment.ServiceCode,
            shipment.RecipientName,
            shipment.ParcelCount,
            entered.TryGetValue(shipment.Id, out int count) ? count : 0,
            shipment.Status,
            shipment.HasInstructions,
            shipment.BookedTotal,
            shipment.BookedCurrencyCode,
            served.Contains(shipment.OriginWarehouseId),
            shipment.CreatedAtCounter,
            shipment.Options.HasFlag(OperationsShipmentOptions.Insured),
            shipment.InsurancePlanId,
            shipment.CustomerSignedUtc,
            shipment.OriginWarehouseId);
}
