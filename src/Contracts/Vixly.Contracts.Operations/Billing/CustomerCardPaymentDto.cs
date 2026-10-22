namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerCardPaymentDto.cs
//
// PURPOSE:
// A customer's card payment as it now stands (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the invoice, the amount, the state (started, paid, cancelled, paid_not_applied) and Stripe's page while it waits
//
// IMPORTANT NOTES:
// - Paid only on Stripe's word
// =========================================================
public sealed record CustomerCardPaymentDto(Guid InvoiceId, string InvoiceNumber, string Status, decimal Amount, string CurrencyCode, string? PageUrl);
