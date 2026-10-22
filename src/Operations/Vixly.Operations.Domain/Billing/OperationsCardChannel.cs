namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardChannel.cs
//
// PURPOSE:
// Where a card payment is made (Slice 5F step 4, owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Name each way
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCardChannel
{
    // Stripe's payment page at the counter: on the customer screen or by a QR code on the customer's phone.
    PaymentPage = 1,

    // A Stripe Terminal card reader at the desk.
    Reader = 2,

    // The customer on the portal.
    Portal = 3
}
