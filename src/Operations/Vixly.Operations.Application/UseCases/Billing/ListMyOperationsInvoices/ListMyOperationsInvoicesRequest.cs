namespace Vixly.Operations.Application.UseCases.Billing.ListMyOperationsInvoices;

// =========================================================
// FILE: ListMyOperationsInvoicesRequest.cs
//
// PURPOSE:
// The customer's own invoices and their approved credit notes (Slice 5F, I-8), on the portal.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListMyOperationsInvoicesRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
