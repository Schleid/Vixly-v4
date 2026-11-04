using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsPreAlert.cs
//
// PURPOSE:
// A customer's notice that a package is on its way (ADR-0022 intake 1).
//
// RESPONSIBILITIES:
// - Carrier, tracking number, store, contents, value and expected date
// - Close it when the package is received, or when the customer cancels
//
// IMPORTANT NOTES:
// - Written by the customer only; staff never type it (D-C6)
// - The tracking number is normalized so the scan matches it
// - Slice 4D: announced on its own for the warehouse it is shipped to, then put into one of the customer's shipments, moved or taken out (owner, 2026-10-19)
// =========================================================
public sealed class OperationsPreAlert : OperationsEntity
{
    private OperationsPreAlert()
    {
        Carrier = string.Empty;
        TrackingNumber = string.Empty;
        Contents = string.Empty;
    }

    private OperationsPreAlert(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Carrier = string.Empty;
        TrackingNumber = string.Empty;
        Contents = string.Empty;
    }

    public Guid CustomerId { get; private set; }

    public string Carrier { get; private set; }

    public string TrackingNumber { get; private set; }

    public string? Store { get; private set; }

    public string Contents { get; private set; }

    public decimal DeclaredValue { get; private set; }

    public string CurrencyCode { get; private set; } = "USD";

    public DateOnly? ExpectedDate { get; private set; }

    public OperationsPreAlertStatus Status { get; private set; }

    public Guid? ParcelId { get; private set; }

    public Guid? ShipmentId { get; private set; }

    // Slice 4D: the warehouse the package is shipped to (the customer's mailbox address there).
    public Guid? WarehouseId { get; private set; }

    public bool IsPlaced => ShipmentId is not null;

    // Slice 4D: announced on its own, for the warehouse it is shipped to.
    public static OperationsPreAlert Announce(Guid tenantId, Guid customerId, Guid warehouseId, string? carrier, string? trackingNumber, string? store, string? contents,
        decimal declaredValue, string currencyCode, DateOnly? expectedDate, DateTimeOffset utcNow)
    {
        if (customerId == Guid.Empty || warehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsShipmentErrors.OriginInvalid);
        }

        return new OperationsPreAlert(tenantId, utcNow)
        {
            CustomerId = customerId,
            WarehouseId = warehouseId,
            Carrier = OperationsReceivingText.Required(carrier, OperationsReceivingLimits.CarrierMaxLength, OperationsReceivingErrors.CarrierInvalid),
            TrackingNumber = OperationsCarrierTracking.Normalize(trackingNumber),
            Store = OperationsReceivingText.Optional(store, OperationsReceivingLimits.StoreMaxLength),
            Contents = OperationsReceivingText.Required(contents, OperationsReceivingLimits.ContentsMaxLength, OperationsReceivingErrors.TextTooLong),
            DeclaredValue = OperationsReceivingText.Value(declaredValue),
            CurrencyCode = currencyCode,
            ExpectedDate = expectedDate
        };
    }

    // Announced straight into one of the customer's shipments (the shipment page).
    public static OperationsPreAlert Create(OperationsShipment shipment, string? carrier, string? trackingNumber, string? store, string? contents, decimal declaredValue,
        string currencyCode, DateOnly? expectedDate, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        OperationsPreAlert preAlert = Announce(shipment.TenantId, shipment.CustomerId, shipment.OriginWarehouseId, carrier, trackingNumber, store, contents, declaredValue, currencyCode,
            expectedDate, utcNow);
        preAlert.PlaceIn(shipment, utcNow);
        return preAlert;
    }

    // Slice 4D: into one of the customer's open shipments from the same warehouse, which says where it goes; moved or taken out later.
    public void PlaceIn(OperationsShipment shipment, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        shipment.EnsureOpen();

        if (Status == OperationsPreAlertStatus.Cancelled)
        {
            throw new DomainException(OperationsReceivingErrors.PreAlertNotOpen);
        }

        if (shipment.TenantId != TenantId || shipment.CustomerId != CustomerId || !shipment.HasInstructions)
        {
            throw new DomainException(OperationsShipmentErrors.InstructionsRequired);
        }

        if (WarehouseId is { } warehouse && warehouse != shipment.OriginWarehouseId)
        {
            throw new DomainException(OperationsShipmentErrors.ParcelElsewhere);
        }

        (ShipmentId, WarehouseId) = (shipment.Id, shipment.OriginWarehouseId);
        MarkAsModified(utcNow);
    }

    public void TakeOut(DateTimeOffset utcNow)
    {
        if (Status == OperationsPreAlertStatus.Cancelled)
        {
            throw new DomainException(OperationsReceivingErrors.PreAlertNotOpen);
        }

        ShipmentId = null;
        MarkAsModified(utcNow);
    }

    public void MarkReceived(Guid parcelId, DateTimeOffset utcNow) => MarkReceived(parcelId, null, utcNow);

    // Slice 4D: received at another warehouse than announced: it belongs there now, and out of a shipment from elsewhere.
    public void MarkReceived(Guid parcelId, Guid? warehouseId, DateTimeOffset utcNow)
    {
        if (Status != OperationsPreAlertStatus.Open)
        {
            throw new DomainException(OperationsReceivingErrors.PreAlertNotOpen);
        }

        if (warehouseId is { } arrivedAt && WarehouseId != arrivedAt)
        {
            (WarehouseId, ShipmentId) = (arrivedAt, null);
        }

        Status = OperationsPreAlertStatus.Received;
        ParcelId = parcelId;
        MarkAsModified(utcNow);
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        if (Status != OperationsPreAlertStatus.Open)
        {
            throw new DomainException(OperationsReceivingErrors.PreAlertNotOpen);
        }

        Status = OperationsPreAlertStatus.Cancelled;
        MarkAsModified(utcNow);
    }
}
