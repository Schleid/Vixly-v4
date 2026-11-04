namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsTenderDraft.cs
//
// PURPOSE:
// One part of a payment as the cashier enters it (Slice 5F, C-3).
//
// RESPONSIBILITIES:
// - Carry the method, the amount, the cash handed over and the reference
//
// IMPORTANT NOTES:
// - Checked when the payment is taken
// =========================================================
public sealed record OperationsTenderDraft(OperationsPaymentMethod Method, decimal Amount, decimal? CashGiven, string? Reference);
