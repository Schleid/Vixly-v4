namespace Vixly.Operations.Application.UseCases.Cashier.CancelOperationsCardPayment;

// =========================================================
// FILE: CancelOperationsCardPaymentRequest.cs
//
// PURPOSE:
// Cancels a card payment that has not been paid (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CancelOperationsCardPaymentRequest
{
    public Guid ChargeId { get; init; }
}
