namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsWebhookEvents.cs
//
// PURPOSE:
// The events a company can receive at a webhook address (Slice 10; owner, 2026-10-22: read and webhooks).
//
// RESPONSIBILITIES:
// - Name each event kind as a flag
//
// IMPORTANT NOTES:
// - Stored as a number; never renumber
// - The wire names are in OperationsWebhookEventNames
// =========================================================
[Flags]
public enum OperationsWebhookEvents
{
    None = 0,

    // A tracking event on a parcel or a shipment (received, departed, arrived, ready, delivered, a hold, a delay...).
    Tracking = 1,

    // A customer signed up or was added.
    CustomerCreated = 2,

    // An invoice was issued.
    InvoiceIssued = 4,

    // A payment was recorded on an invoice.
    PaymentRecorded = 8
}
