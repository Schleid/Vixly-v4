using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Billing.ListOperationsInvoices;

// =========================================================
// FILE: ListOperationsInvoicesRequest.cs
//
// PURPOSE:
// The invoices of the warehouses a person sees (Slice 5F, I-8): find by number, customer or shipment, filter by state.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsInvoicesRequest
{
    public string? Text { get; init; }

    public OperationsInvoiceFilter Filter { get; init; }
}
