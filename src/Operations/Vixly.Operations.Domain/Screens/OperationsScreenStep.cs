namespace Vixly.Operations.Domain.Screens;

// =========================================================
// FILE: OperationsScreenStep.cs
//
// PURPOSE:
// What a customer screen shows now (Slice 5F step 3, P-2, P-3, P-9).
//
// RESPONSIBILITIES:
// - Name each step
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsScreenStep
{
    // The company's name only, waiting for the next customer.
    Idle = 0,

    // The invoice's summary and the payment methods the customer may use on the screen.
    Summary = 1,

    // Zelle: the warning and the company's terms, to accept.
    ZelleTerms = 2,

    // Zelle: the QR code, tag, contact, amount and the number for the memo.
    Zelle = 3,

    // A card on Stripe's page.
    Card = 4,

    // The shipment's declaration and insurance, to sign.
    Signature = 5,

    // Signed, or paid: thank you.
    Done = 6
}
