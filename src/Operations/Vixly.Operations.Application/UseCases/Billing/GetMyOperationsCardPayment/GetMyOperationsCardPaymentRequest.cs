namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCardPayment;

// =========================================================
// FILE: GetMyOperationsCardPaymentRequest.cs
//
// PURPOSE:
// A customer's card payment as it now stands, when they come back from Stripe's page (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsCardPaymentRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ChargeId { get; init; }
}
