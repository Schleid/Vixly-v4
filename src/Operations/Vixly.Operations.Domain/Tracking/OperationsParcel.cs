using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsParcel.cs
//
// PURPOSE:
// One parcel: a physical package in a tenant's care. A parcel belongs to a shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Keep the projection of its tracking events: milestone, flags, open holds
// - Keep what receiving learned: number, carrier and tracking number, measurements, declaration, shelf
//
// IMPORTANT NOTES:
// - The state changes only through Project, in the same commit as the event (D-R6)
// - An unidentified parcel has no customer and no shipment until a clerk assigns it (D-R8)
// =========================================================
public sealed class OperationsParcel : OperationsEntity
{
    private OperationsParcel()
    {
    }

    private OperationsParcel(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public string? Number { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? ShipmentId { get; private set; }

    public Guid? WarehouseId { get; private set; }

    public string? Carrier { get; private set; }

    public string? CarrierTrackingNumber { get; private set; }

    public decimal? WeightKg { get; private set; }

    public decimal? LengthCm { get; private set; }

    public decimal? WidthCm { get; private set; }

    public decimal? HeightCm { get; private set; }

    public string? DeclaredContents { get; private set; }

    public decimal? DeclaredValue { get; private set; }

    public string? CurrencyCode { get; private set; }

    public Guid? PreAlertId { get; private set; }

    public Guid? StorageLocationId { get; private set; }

    // The carrier delivery it arrived with (its warehouse receipt).
    public Guid? DeliveryId { get; private set; }

    // Damage seen at the dock, for the warehouse receipt the driver signs.
    public bool ArrivedDamaged { get; private set; }

    // Warehouse and carrier tracking number while the parcel is at its origin; the database allows one such parcel (review H2).
    public string? OriginKey { get; private set; }

    public OperationsMilestone? Milestone { get; private set; }

    public OperationsParcelFlags Flags { get; private set; }

    public OperationsHoldReasons HoldReasons { get; private set; }

    public DateTimeOffset? LastEventUtc { get; private set; }

    // Reminders sent while the package waits for its customer (Slice 3E, Q-R6): 0 to 4, the last one the final notice.
    public int RemindersSent { get; private set; }

    public OperationsParcelState State => new(Milestone, Flags, HoldReasons, LastEventUtc);

    public bool IsIdentified => CustomerId is not null;

    // Slice 4D step 3: what the label of an undeclared package shows, recorded by staff: the name and mailbox code written on it, the sender.
    public string? LabelName { get; private set; }

    public string? LabelCode { get; private set; }

    public string? LabelSender { get; private set; }

    // Slice 4D step 3 (review): the storage terms in force when the package arrived; later changes never apply backwards.
    public int? StorageFreeDays { get; private set; }

    public decimal? StorageFeePerDay { get; private set; }

    public void TakeStorageTerms(int freeDays, decimal? feePerDay)
    {
        (StorageFreeDays, StorageFeePerDay) = (freeDays, feePerDay);
    }

    // The fee runs while the package waits at its warehouse for its customer or its owner, not once it is booked to leave (Slice 5).
    public bool OwesStorage => Milestone is null or OperationsMilestone.ReceivedAtOrigin;

    public void RecordLabel(string? name, string? code, string? sender, DateTimeOffset utcNow)
    {
        (LabelName, LabelCode, LabelSender) = (LabelText(name, 100), LabelText(code, 20)?.ToUpperInvariant(), LabelText(sender, 100));
        MarkAsModified(utcNow);
    }

    private static string? LabelText(string? value, int max)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null || (text.Length <= max && !text.Any(char.IsControl) && text.IndexOfAny(['<', '>']) < 0)
            ? text
            : throw new DomainException(Vixly.Operations.Domain.Holds.OperationsHoldErrors.LabelTextInvalid);
    }

    // Slice 4D step 2: when its shipping label was last printed (or downloaded to print).
    public DateTimeOffset? ShippingLabelUtc { get; private set; }

    // Cleared: checked, no hold, no compliance inspection waiting, still here, in one of the customer's shipments (A-5).
    public bool IsClearedIn(OperationsShipment? shipment) =>
        Milestone is >= OperationsMilestone.ReceivedAtOrigin and < OperationsMilestone.DepartedOrigin && HoldReasons == OperationsHoldReasons.None
        && !Flags.HasFlag(OperationsParcelFlags.AwaitingCheck) && !Flags.HasFlag(OperationsParcelFlags.InspectionPending)
        && shipment is { HasInstructions: true, Status: OperationsShipmentStatus.Open or OperationsShipmentStatus.Booked } && shipment.Id == ShipmentId;

    // The shipment's instructions changed (recipient, delivery, service): the printed label no longer fits.
    public void ShippingLabelOutdated(DateTimeOffset utcNow)
    {
        if (ShippingLabelUtc is not null)
        {
            ShippingLabelUtc = null;
            MarkAsModified(utcNow);
        }
    }

    public void ShippingLabelPrinted(DateTimeOffset utcNow)
    {
        ShippingLabelUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // A bare parcel, for the tracking core and its tests.
    public static OperationsParcel Create(Guid tenantId, Guid? customerId, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { CustomerId = customerId };

    // Slice 5C: a box handed over at the front-desk counter, for the customer's shipment there; weighed, measured and described in front of the customer.
    public static OperationsParcel DropOff(Guid tenantId, string number, OperationsShipment shipment, OperationsMeasurements measurements, string? contents, decimal? value,
        string currencyCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        OperationsParcel parcel = new(tenantId, utcNow) { Number = number, WarehouseId = shipment.OriginWarehouseId };
        parcel.JoinShipment(shipment, utcNow);
        parcel.Measure(measurements, utcNow);
        (parcel.DeclaredContents, parcel.DeclaredValue, parcel.CurrencyCode) = (
            OperationsReceivingText.Required(contents, OperationsReceivingLimits.ContentsMaxLength, OperationsReceivingErrors.ContentsRequired),
            OperationsReceivingText.Value(value ?? -1m), currencyCode);
        return parcel;
    }

    // A package scanned in at a receiving warehouse (ADR-0022 intake 1).
    // A package scanned in at the dock with its carrier delivery (ADR-0022 intake 1); weight and size follow at the inspection station.
    public static OperationsParcel Receive(Guid tenantId, string number, OperationsInboundDelivery delivery, string? carrier, string carrierTrackingNumber, bool damaged, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentNullException.ThrowIfNull(delivery);
        delivery.TakePiece(utcNow);
        string tracking = OperationsCarrierTracking.Normalize(carrierTrackingNumber);

        return new OperationsParcel(tenantId, utcNow)
        {
            Number = number,
            WarehouseId = delivery.WarehouseId,
            DeliveryId = delivery.Id,
            ArrivedDamaged = damaged,
            Carrier = OperationsReceivingText.Optional(carrier, OperationsReceivingLimits.CarrierMaxLength),
            CarrierTrackingNumber = tracking,
            OriginKey = OriginKeyOf(delivery.WarehouseId, tracking)
        };
    }

    // Weighed and measured at the inspection station; a second check corrects the first.
    public void Measure(OperationsMeasurements measurements, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        // Slice 4D step 2 (review): a new weight makes the printed shipping label wrong; it goes back to the labels to print.
        if (WeightKg != measurements.WeightKg)
        {
            ShippingLabelUtc = null;
        }

        (WeightKg, LengthCm, WidthCm, HeightCm) = (measurements.WeightKg, measurements.LengthCm, measurements.WidthCm, measurements.HeightCm);
        MarkAsModified(utcNow);
    }

    // The parcel travels with this shipment and belongs to its customer (D-R1).
    public void JoinShipment(OperationsShipment shipment, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        shipment.EnsureOpen();

        // Never into another customer's shipment; a parcel not yet booked may move to another shipment of its customer.
        if (shipment.TenantId != TenantId || (WarehouseId is { } warehouse && warehouse != shipment.OriginWarehouseId)
            || (CustomerId is { } owner && owner != shipment.CustomerId) || Milestone > OperationsMilestone.ReceivedAtOrigin)
        {
            throw new DomainException(OperationsReceivingErrors.ShipmentNotOpen);
        }

        // Slice 4D step 2: a shipping label printed for another shipment no longer fits.
        if (ShipmentId != shipment.Id)
        {
            ShippingLabelUtc = null;
        }

        (ShipmentId, CustomerId) = (shipment.Id, shipment.CustomerId);
        MarkAsModified(utcNow);
    }

    // The customer's own declaration, from their pre-alert (D-C6: never typed by staff).
    public void MatchPreAlert(OperationsPreAlert preAlert, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(preAlert);

        // Only the parcel's own customer's pre-alert, for this tracking number (review M5).
        if (preAlert.TenantId != TenantId || preAlert.CustomerId != CustomerId || preAlert.TrackingNumber != CarrierTrackingNumber)
        {
            throw new DomainException(OperationsReceivingErrors.PreAlertNotOpen);
        }

        preAlert.MarkReceived(Id, WarehouseId, utcNow);
        (PreAlertId, DeclaredContents, DeclaredValue, CurrencyCode) = (preAlert.Id, preAlert.Contents, preAlert.DeclaredValue, preAlert.CurrencyCode);
        Carrier ??= preAlert.Carrier;
        MarkAsModified(utcNow);
    }

    // The customer confirmed or corrected contents and value, and staff accepted it (Slice 3E).
    public void Declare(string contents, decimal value, string currencyCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contents);
        (DeclaredContents, DeclaredValue, CurrencyCode) = (contents, value, currencyCode);
        MarkAsModified(utcNow);
    }

    // One more reminder sent; true when it was the one due.
    public bool Remind(int sent, DateTimeOffset utcNow)
    {
        if (RemindersSent != sent - 1 || sent > Vixly.Operations.Domain.Holds.OperationsHoldLimits.ReminderDays.Count)
        {
            return false;
        }

        RemindersSent = sent;
        MarkAsModified(utcNow);
        return true;
    }

    public void PutAway(OperationsStorageLocation location, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!location.IsActive || location.WarehouseId != WarehouseId || location.TenantId != TenantId)
        {
            throw new DomainException(OperationsReceivingErrors.LocationInactive);
        }

        StorageLocationId = location.Id;
        MarkAsModified(utcNow);
    }

    public void Project(OperationsParcelState state, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(state);
        (Milestone, Flags, HoldReasons, LastEventUtc) = (state.Milestone, state.Flags, state.HoldReasons, state.LastOccurredUtc);

        // Gone from the origin (departed, cancelled, returned or disposed of): the number may arrive there again.
        if (!IsAtOrigin)
        {
            OriginKey = null;
        }

        MarkAsModified(utcNow);
    }

    public bool IsAtOrigin => Milestone is null || Milestone < OperationsMilestone.DepartedOrigin;

    public static string OriginKeyOf(Guid warehouseId, string carrierTrackingNumber) => warehouseId.ToString("N") + ":" + carrierTrackingNumber;
}
