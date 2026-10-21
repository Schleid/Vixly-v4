namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsNumberKind.cs
//
// PURPOSE:
// The kinds of numbers a tenant issues, each with its own prefix and yearly sequence (D-O12).
//
// RESPONSIBILITIES:
// - Name each numbered document
//
// IMPORTANT NOTES:
// - Stored as int; never renumber; later slices add kinds
// =========================================================
public enum OperationsNumberKind
{
    Shipment = 1,
    Receiving = 2,
    Invoice = 3,
    Payment = 4,

    // One sequence for all years (Slice 2, D-C1).
    Customer = 5,

    // Slice 3B: the ID barcode of each parcel.
    Parcel = 6,

    // Slice 4: a quote made by staff (QT + year + six digits).
    Quote = 7,

    // Slice 5B: an insurance certificate (IC + year + six digits).
    Certificate = 8,

    // Slice 5E: an order of supplies on its own (the company's prefix, SO by default).
    SupplyOrder = 9,

    // Slice 5F: an approved credit note (the company's prefix, CN by default).
    CreditNote = 10,

    // Slice 6A: a bundle (BD by default) and a repack box (CB by default).
    Bundle = 11,

    Box = 12,

    // Slice 6B: a dispatch (DSP by default).
    Dispatch = 13
}
