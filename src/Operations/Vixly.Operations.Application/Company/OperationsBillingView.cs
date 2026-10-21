using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Company;

// =========================================================
// FILE: OperationsBillingView.cs
//
// PURPOSE:
// What the billing page shows (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the plan, the invoices and the billing PDFs
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsBillingView(OperationsSubscription? Subscription, IReadOnlyList<OperationsInvoice> Invoices, int InvoiceCount, IReadOnlyList<OperationsBillingDocument> Documents, int DocumentCount);
