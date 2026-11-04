using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentPlacement.cs
//
// PURPOSE:
// Chooses the shipment a received parcel joins (owner, 2026-10-18; Slice 4D, owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - The shipment the customer put the package's pre-alert in, when it is open at the parcel's warehouse
// - Otherwise the customer's waiting shipment there (no instructions), created when needed: a parcel always belongs to a shipment
//
// IMPORTANT NOTES:
// - Never placed by guesswork: a customer may have many open shipments, and only the pre-alert says which one
// - The chosen shipment's row changes, so a cancel at the same moment clashes on its row version
// =========================================================
public sealed class OperationsShipmentPlacement
{
    private readonly IOperationsReceivingRepository _receiving;

    public OperationsShipmentPlacement(IOperationsReceivingRepository receiving)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    // Placed: the pre-alert's own shipment took the parcel; otherwise the waiting shipment did.
    public async Task<(OperationsShipment Shipment, bool Placed)> PlaceAsync(Guid tenantId, Guid customerId, Guid warehouseId, OperationsPreAlert? preAlert, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (preAlert?.ShipmentId is { } placedIn
            && await _receiving.GetShipmentAsync(placedIn, cancellationToken) is { Status: OperationsShipmentStatus.Open, HasInstructions: true } chosen
            && chosen.OriginWarehouseId == warehouseId && chosen.CustomerId == customerId)
        {
            chosen.TakePackage(utcNow);
            return (chosen, true);
        }

        return (await WaitingAsync(tenantId, customerId, warehouseId, utcNow, cancellationToken), false);
    }

    // The customer's waiting shipment at the warehouse.
    public async Task<OperationsShipment> WaitingAsync(Guid tenantId, Guid customerId, Guid warehouseId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        OperationsShipment? waiting = await _receiving.FindOpenShipmentAsync(customerId, warehouseId, cancellationToken);

        if (waiting is null)
        {
            waiting = OperationsShipment.Open(tenantId, customerId, warehouseId, utcNow);
            _receiving.Add(waiting);
        }

        waiting.TakePackage(utcNow);
        return waiting;
    }
}
