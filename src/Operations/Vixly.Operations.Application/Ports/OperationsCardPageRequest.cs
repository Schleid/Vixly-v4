namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCardPageRequest.cs
//
// PURPOSE:
// What Stripe is asked to take for an invoice (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the amount, currency, what it is for, where to come back to, and the charge's id for Stripe to give back
//
// IMPORTANT NOTES:
// - The charge id travels as metadata (vixly_charge) and as the idempotency key
// =========================================================
public sealed record OperationsCardPageRequest(Guid ChargeId, decimal Amount, string CurrencyCode, string Description, string? Email, string? ReturnUrl);
