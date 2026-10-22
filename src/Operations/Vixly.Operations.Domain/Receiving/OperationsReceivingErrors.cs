using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsReceivingErrors.cs
//
// PURPOSE:
// Error codes of receiving (Slice 3B).
//
// RESPONSIBILITIES:
// - Name every refusal about pre-alerts, parcels, receipts and storage locations
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsReceivingErrors
{
    public static Error TrackingNumberInvalid { get; } = Error.Create("ops.receiving.tracking_number_invalid");

    public static Error CarrierInvalid { get; } = Error.Create("ops.receiving.carrier_invalid");

    public static Error TextTooLong { get; } = Error.Create("ops.receiving.text_too_long");

    public static Error ValueInvalid { get; } = Error.Create("ops.receiving.value_invalid");

    // Slice 5C: a parcel at the counter says what is in it.
    public static Error ContentsRequired { get; } = Error.Create("ops.receiving.contents_required");

    public static Error MeasurementsInvalid { get; } = Error.Create("ops.receiving.measurements_invalid");

    public static Error PreAlertNotOpen { get; } = Error.Create("ops.receiving.pre_alert_not_open");

    public static Error PreAlertDuplicate { get; } = Error.Create("ops.receiving.pre_alert_duplicate");

    public static Error LocationCodeInvalid { get; } = Error.Create("ops.receiving.location_code_invalid");

    public static Error LocationInactive { get; } = Error.Create("ops.receiving.location_inactive");

    public static Error LocationDuplicate { get; } = Error.Create("ops.receiving.location_duplicate");

    public static Error WarehouseNotReceiving { get; } = Error.Create("ops.receiving.warehouse_not_receiving");

    public static Error CustomerNotFound { get; } = Error.Create("ops.receiving.customer_not_found");

    public static Error NotUnidentified { get; } = Error.Create("ops.receiving.not_unidentified");

    public static Error VerificationRequired { get; } = Error.Create("ops.receiving.verification_required");

    public static Error PhotoRequired { get; } = Error.Create("ops.receiving.photo_required");

    public static Error PhotoInvalid { get; } = Error.Create("ops.receiving.photo_invalid");

    public static Error ConditionInvalid { get; } = Error.Create("ops.receiving.condition_invalid");

    public static Error ShipmentNotOpen { get; } = Error.Create("ops.receiving.shipment_not_open");

    // A missing parcel and one the reader may not see give the same answer (S-3).
    public static Error ParcelNotFound { get; } = Error.Create("ops.receiving.parcel_not_found");

    // Cancelled, returned, disposed of, or already gone from its origin warehouse.
    public static Error ParcelClosed { get; } = Error.Create("ops.receiving.parcel_closed");

    public static Error PreAlertNotFound { get; } = Error.Create("ops.receiving.pre_alert_not_found");

    public static Error ExpectedDateInvalid { get; } = Error.Create("ops.receiving.expected_date_invalid");

    // Slice 3B scan station and inspection station (owner, 2026-10-18).
    public static Error DeliveryClosed { get; } = Error.Create("ops.receiving.delivery_closed");

    public static Error DeliveryNotFound { get; } = Error.Create("ops.receiving.delivery_not_found");

    public static Error BarcodeUnreadable { get; } = Error.Create("ops.receiving.barcode_unreadable");

    public static Error NotAwaitingCheck { get; } = Error.Create("ops.receiving.not_awaiting_check");

    // The same carrier tracking number is already in this warehouse (parcel number given).
    public static Error AlreadyReceived(string? number) => Error.Create("ops.receiving.already_received", ("number", number ?? string.Empty));
}
