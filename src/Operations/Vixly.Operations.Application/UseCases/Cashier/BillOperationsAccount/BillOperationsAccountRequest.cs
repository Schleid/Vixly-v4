namespace Vixly.Operations.Application.UseCases.Cashier.BillOperationsAccount;

// =========================================================
// FILE: BillOperationsAccountRequest.cs
//
// PURPOSE:
// The cashier bills an invoice to the customer's credit account (Slice 5F step 2b, K-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BillOperationsAccountRequest
{
    public Guid InvoiceId { get; init; }
}
