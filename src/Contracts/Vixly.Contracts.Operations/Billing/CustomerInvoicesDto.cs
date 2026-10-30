namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerInvoicesDto.cs
//
// PURPOSE:
// The customer's invoices (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Carry the list
//
// IMPORTANT NOTES:
// - Newest first, up to 100
// - CardOffered (Slice 5F step 4): the company takes cards on Stripe's page
// =========================================================
public sealed record CustomerInvoicesDto(IReadOnlyList<CustomerInvoiceDto> Invoices, bool CardOffered = false);
