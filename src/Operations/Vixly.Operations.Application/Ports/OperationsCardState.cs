namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCardState.cs
//
// PURPOSE:
// Where a Stripe payment stands, read from Stripe (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Say whether it is paid, with the payment (pi_...), the amount and currency taken; or over without payment
//
// IMPORTANT NOTES:
// - Paid means the money is taken (payment_status paid, or a succeeded payment)
// =========================================================
public sealed record OperationsCardState(bool Paid, bool Over, string? PaymentIntentId, decimal Amount, string? CurrencyCode)
{
    // Stripe's livemode: a test payment never settles a live account's invoice (review).
    public bool? Live { get; init; }
}
