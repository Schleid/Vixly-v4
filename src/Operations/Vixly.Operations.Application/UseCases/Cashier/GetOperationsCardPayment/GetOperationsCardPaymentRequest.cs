namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardPayment;

// =========================================================
// FILE: GetOperationsCardPaymentRequest.cs
//
// PURPOSE:
// A card payment at the cashier as it now stands, asking Stripe while it waits (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsCardPaymentRequest
{
    public Guid ChargeId { get; init; }
}
