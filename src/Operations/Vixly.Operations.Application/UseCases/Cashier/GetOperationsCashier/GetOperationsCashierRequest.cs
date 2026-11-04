using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashier;

// =========================================================
// FILE: GetOperationsCashierRequest.cs
//
// PURPOSE:
// The cashier (Slice 5F, C-2): find a customer by phone, email or code, or an invoice by its number or its shipment's or order's; their invoices to pay; the person's open drawers.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCashierRequest
{
    public string? Find { get; init; }

    public Guid? CustomerId { get; init; }
}
