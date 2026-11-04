namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsAccountCharge.cs
//
// PURPOSE:
// Whether an invoice is billed to the customer's credit account (Slice 5F step 2b, K-3 and K-4).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Charged releases the shipment; the invoice stays owed until paid
// =========================================================
public enum OperationsAccountCharge
{
    None = 0,
    AwaitingApproval = 1,
    Charged = 2
}
