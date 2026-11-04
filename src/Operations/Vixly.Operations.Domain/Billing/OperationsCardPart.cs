namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardPart.cs
//
// PURPOSE:
// The card part of a payment made of several methods: what Stripe confirmed (owner, 2026-10-26).
//
// RESPONSIBILITIES:
// - Carry the amount Stripe took and its payment reference (pi_...)
//
// IMPORTANT NOTES:
// - Only Stripe's word makes one (OperationsCardDesk); a cashier never types a card
// =========================================================
public sealed record OperationsCardPart(decimal Amount, string PaymentIntentId);
